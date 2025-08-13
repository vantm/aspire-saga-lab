using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqManager(IOptions<RabbitMqOptions> options, RabbitMqInstances instances) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = options.Value.Uri,
        };

        instances.Connection = await connectionFactory.CreateConnectionAsync();
        instances.ServiceBusChannel = await instances.Connection.CreateChannelAsync();
        instances.MessageConsumerChannel = await instances.Connection.CreateChannelAsync();

        foreach (var eventType in options.Value.EventTypes)
        {
            await RegisterEvent(eventType, instances.ServiceBusChannel);
        }
    }

    public async Task RegisterEvent(Type eventType, IChannel channel)
    {
        var name = eventType.FullName!.Underscore();
        var deadLetterName = $"{name}.dead_letters";

        await channel.ExchangeDeclareAsync(name, type: "fanout", durable: true, autoDelete: false);
        await channel.QueueDeclareAsync(name, durable: true, exclusive: false, autoDelete: true, arguments: new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", deadLetterName  },
            { "x-message-ttl", 86400000  }
        });
        await channel.QueueBindAsync(name, name, routingKey: string.Empty);

        await channel.ExchangeDeclareAsync(deadLetterName, type: "fanout", durable: true, autoDelete: false);
        await channel.QueueDeclareAsync(deadLetterName, durable: true, exclusive: false, autoDelete: true);
        await channel.QueueBindAsync(deadLetterName, deadLetterName, routingKey: string.Empty);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            instances.Connection.DisposeAsync().AsTask(),
            instances.ServiceBusChannel.DisposeAsync().AsTask(),
            instances.MessageConsumerChannel.DisposeAsync().AsTask());
    }
}

