using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqWorker<T, E>(RabbitMqInstances instances, IServiceProvider services, ILogger<RabbitMqWorker<T, E>> logger)
    : IHostedService
    where T : class, IConsumer<E>
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

        var queueName = instances.GetQueueOrExchangeName(typeof(E));

        _consumerTag = await instances.MessageConsumerChannel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task ReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        var senderChannel = ((AsyncEventingBasicConsumer)sender).Channel;
        try
        {
            var body = @event.Body.ToArray();

            var messageType = Encoding.UTF8.GetString((byte[])@event.BasicProperties.Headers?["MessageType"]!);
            Debug.Assert(messageType is not null, "The message type cannot be null.");

            Type type = Type.GetType(messageType)!;
            Debug.Assert(type is not null, $"The message type {messageType} could not be resolved.");

            var message = MessagePackSerializer.Deserialize(type, body, cancellationToken: @event.CancellationToken);
            Debug.Assert(message is not null, "The deserialized message cannot be null.");

            var consumerType = typeof(IConsumer<>).MakeGenericType(type);
            var consumeMethod = consumerType.GetMethod(nameof(IConsumer<object>.ConsumeAsync));
            Debug.Assert(consumeMethod is not null, "The ConsumeAsync method could not be found on the consumer type.");

            await using var scope = services.CreateAsyncScope();

            var consumers = scope.ServiceProvider.GetServices(consumerType);

            foreach (var consumer in consumers)
            {
                Debug.Assert(consumer is not null, "The consumer cannot be null.");
                await (Task)consumeMethod.Invoke(consumer, [message, @event.CancellationToken])!;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing the message {MessageId}.", @event.DeliveryTag);
            await senderChannel.BasicRejectAsync(@event.DeliveryTag, requeue: false, @event.CancellationToken);
        }
        finally
        {
            await senderChannel.BasicAckAsync(@event.DeliveryTag, multiple: false, @event.CancellationToken);
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
