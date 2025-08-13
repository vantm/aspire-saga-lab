using MessagePack;
using RabbitMQ.Client;
using System.Diagnostics;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqServiceBus(RabbitMqInstances instances) : IServiceBus
{
    public async Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        Debug.Assert(message is not null, "The message cannot be null.");

        var eventType = message.GetType();

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

        var exchangeName = instances.GetQueueOrExchangeName(eventType);

        await instances.ServiceBusChannel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
