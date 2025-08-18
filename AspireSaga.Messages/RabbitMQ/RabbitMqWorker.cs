using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqWorker<T>(RabbitMqInstances instances, IServiceProvider services, ILogger<RabbitMqWorker<T>> logger, ActivitySource source) : IHostedService
{
    private string? _consumerTag;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (instances.MessageConsumerChannel is null)
        {
            // Wait for the channel to be created
            await Task.Delay(100, cancellationToken);
        }

        AsyncEventingBasicConsumer consumer = new(instances.MessageConsumerChannel);

        consumer.ReceivedAsync += ReceivedAsync;

        var queueName = instances.GetQueueName(typeof(T));

        _consumerTag = await instances.MessageConsumerChannel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task ReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        var senderChannel = ((AsyncEventingBasicConsumer)sender).Channel;
        Activity? activity = null;

        try
        {
            var body = @event.Body.ToArray();

            var messageTypeInString = Encoding.UTF8.GetString((byte[])@event.BasicProperties.Headers?["MessageType"]!);
            Debug.Assert(messageTypeInString is not null, "The message type cannot be null.");

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Received message of type {MessageType} with delivery tag {DeliveryTag}.", messageTypeInString, @event.DeliveryTag);
            }

            Type messageType = Type.GetType(messageTypeInString)!;
            Debug.Assert(messageType is not null, $"The message type {messageTypeInString} could not be resolved.");

            var message = MessagePackSerializer.Deserialize(messageType, body, cancellationToken: @event.CancellationToken);
            Debug.Assert(message is not null, "The deserialized message cannot be null.");

            if (logger.IsEnabled(LogLevel.Debug))
            {
                var json = JsonSerializer.Serialize(message);
                logger.LogDebug("Deserialized message: {Message}", json);
            }

            var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);
            var consumeMethod = consumerType.GetMethod(nameof(IConsumer<object>.ConsumeAsync));
            Debug.Assert(consumeMethod is not null, "The ConsumeAsync method could not be found on the consumer type.");

            var serviceBus = services.GetRequiredService<IServiceBus>();
            if (serviceBus is not RabbitMqServiceBus sb)
            {
                throw new InvalidOperationException("The service provider is not a RabbitMqServiceBus instance.");
            }

            await using var scope = services.CreateAsyncScope();

            var subscriptions = sb.GetSubscriptions(messageType);

            var parentContext = Propagators.DefaultTextMapPropagator.Extract(default, @event.BasicProperties.Headers, (carrier, key) =>
            {
                if (carrier is { } && carrier.TryGetValue(key, out var value) && value is byte[] bytes)
                {
                    return [Encoding.UTF8.GetString(bytes)];
                }
                return [];
            });

            if (subscriptions.Any())
            {
                activity = source.StartActivity("RabbitMqWorker", ActivityKind.Consumer, parentContext.ActivityContext);
            }

            activity?.SetTag("event-type", messageTypeInString);

            foreach (var subscription in subscriptions)
            {
                Debug.Assert(subscription is { Factory: not null }, "The consumer cannot be null.");

                var consumer = subscription.Factory(scope.ServiceProvider);

                activity?.AddTag("consumers", consumer.GetType().Name);

                await (Task)consumeMethod.Invoke(consumer, [message, @event.CancellationToken])!;
            }

            await senderChannel.BasicAckAsync(@event.DeliveryTag, multiple: false, @event.CancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.AddTag("exception.message", ex.Message);
            activity?.AddTag("exception.stack-trace", ex.StackTrace);
            activity?.SetStatus(ActivityStatusCode.Error);

            logger.LogError(ex, "An error occurred while processing the message {MessageId}.", @event.DeliveryTag);

            await senderChannel.BasicRejectAsync(@event.DeliveryTag, requeue: false, @event.CancellationToken);
        }
        finally
        {
            activity?.Stop();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumerTag is not null)
        {
            await instances.MessageConsumerChannel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            _consumerTag = null;
        }
    }
}
