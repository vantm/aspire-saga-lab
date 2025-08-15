using MessagePack;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqServiceBus(RabbitMqInstances instances, ILogger<RabbitMqServiceBus> logger, ActivitySource source) : IServiceBus
{
    public record Subscription(Guid Id, Func<IServiceProvider, object> Factory);

    private readonly Dictionary<Type, List<Subscription>> _registry = [];

    public IEnumerable<Subscription> GetSubscriptions(Type eventType)
    {
        if (_registry.TryGetValue(eventType, out var subscriptions))
        {
            return subscriptions.AsReadOnly();
        }
        return [];
    }

    public async Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        var activity = source.StartActivity("RabbitMqServiceBus.PublishAsync", ActivityKind.Producer);

        Debug.Assert(message is not null, "The message cannot be null.");

        var eventType = message.GetType();

        activity?.SetTag("event-type", eventType.AssemblyQualifiedName);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var json = JsonSerializer.Serialize(message);

            logger.LogDebug("Publishing message of type {EventType} with content: {Message}", eventType.Name, json);

            activity?.SetTag("message", json);
        }

        var basicProperties = new BasicProperties()
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                { "MessageType", eventType.AssemblyQualifiedName },
            }
        };

        var body = MessagePackSerializer.Serialize(message, cancellationToken: cancellationToken);

        var exchangeName = instances.GetExchangeName(eventType);

        activity?.SetTag("exchange-name", exchangeName);

        var propagator = Propagators.DefaultTextMapPropagator;
        var propagationContext = new PropagationContext(activity?.Context ?? default, Baggage.Current);

        propagator.Inject(propagationContext, basicProperties.Headers, (carrier, key, value) =>
        {
            carrier[key] = value;
        });

        await instances.ServiceBusChannel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: cancellationToken);

        activity?.Stop();
    }

    public async Task<TReply> GetReplyAsync<TReply>(object message, CancellationToken cancellationToken = default)
        where TReply : IReply
    {
        var activity = source.StartActivity("RabbitMqServiceBus.GetReplyAsync", ActivityKind.Producer);

        Debug.Assert(message is not null, "The message cannot be null.");

        var eventType = message.GetType();

        activity?.SetTag("event-type", eventType.AssemblyQualifiedName);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var json = JsonSerializer.Serialize(message);
            activity?.SetTag("message", json);
        }

        var basicProperties = new BasicProperties()
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                { "MessageType", eventType.AssemblyQualifiedName },
                { "ReplyType", typeof(TReply).AssemblyQualifiedName },
            },
        };

        var body = MessagePackSerializer.Serialize(message, cancellationToken: cancellationToken);

        var exchangeName = instances.GetExchangeName(eventType);

        activity?.SetTag("exchange-name", exchangeName);

        var propagator = Propagators.DefaultTextMapPropagator;
        var propagationContext = new PropagationContext(activity?.Context ?? default, Baggage.Current);

        propagator.Inject(propagationContext, basicProperties.Headers, (carrier, key, value) =>
        {
            carrier[key] = value;
        });

        await instances.ServiceBusChannel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: cancellationToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var _ = cancellationToken.Register(() => cts.Cancel());

        var queueName = instances.GetQueueName(typeof(TReply));
        var result = await instances.ServiceBusChannel.BasicGetAsync(queueName, autoAck: true, cts.Token);
        var reply = MessagePackSerializer.Deserialize<TReply>(result!.Body, cancellationToken: cancellationToken);

        activity?.Stop();

        return reply!;
    }

    public Guid Subscribe<TEvent>(Func<IServiceProvider, IConsumer<TEvent>> factory)
    {
        var eventType = typeof(TEvent);
        var subscriptionId = Guid.NewGuid();
        if (!_registry.TryGetValue(eventType, out var subscriptions))
        {
            _registry[eventType] = [new(subscriptionId, factory)];
        }
        else
        {
            _registry[eventType].Add(new(subscriptionId, factory));
        }
        return subscriptionId;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        foreach (var (eventType, subscriptions) in _registry)
        {
            subscriptions.RemoveAll(s => s.Id == subscriptionId);
            if (subscriptions.Count == 0)
            {
                _registry.Remove(eventType);
            }
        }
    }
}
