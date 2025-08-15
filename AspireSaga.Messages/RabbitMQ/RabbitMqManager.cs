using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqManager(IOptions<RabbitMqOptions> options, RabbitMqInstances instances, IConnection connection) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        instances.ServiceBusChannel = await connection.CreateChannelAsync();
        instances.MessageConsumerChannel = await connection.CreateChannelAsync();

        using var channel = await connection.CreateChannelAsync();

        foreach (var eventType in options.Value.EventTypes)
        {
            await RegisterEvent(eventType, channel);
        }
    }

    public async Task RegisterEvent(Type eventType, IChannel channel)
    {
        await ExchangeDeclare();
        await QueueDeclare();

        async Task ExchangeDeclare()
        {
            var name = instances.GetExchangeName(eventType);
            var deadLetterName = instances.GetDeadLetterExchangeName(eventType);

            await channel.ExchangeDeclareAsync(name, type: "fanout", durable: true, autoDelete: false);
            await channel.ExchangeDeclareAsync(deadLetterName, type: "fanout", durable: true, autoDelete: false);
        }

        async Task QueueDeclare()
        {
            var exchangeName = instances.GetExchangeName(eventType);
            var queueName = instances.GetQueueName(eventType);

            var deadLetterExchangeName = instances.GetDeadLetterExchangeName(eventType);
            var deadLetterQueueName = instances.GetDeadLetterQueueName(eventType);

            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: true, autoDelete: true, arguments: new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", deadLetterQueueName  },
                { "x-message-ttl", 86400000  }
            });
            await channel.QueueDeclareAsync(deadLetterQueueName, durable: true, exclusive: true, autoDelete: true);

            await channel.QueueBindAsync(queueName, exchangeName, routingKey: string.Empty);
            await channel.QueueBindAsync(deadLetterQueueName, deadLetterExchangeName, routingKey: string.Empty);

        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            instances.ServiceBusChannel.DisposeAsync().AsTask(),
            instances.MessageConsumerChannel.DisposeAsync().AsTask());
    }
}

