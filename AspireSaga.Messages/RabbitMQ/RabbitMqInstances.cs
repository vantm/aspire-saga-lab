using Humanizer;
using MessagePack.Resolvers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Threading.Tasks.Sources;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqInstances(IOptions<RabbitMqOptions> options)
{
    public IChannel ServiceBusChannel { get; set; } = default!;
    public IChannel MessageConsumerChannel { get; set; } = default!;

    public string GetExchangeName(Type eventType)
    {
        return eventType.FullName!.Underscore();
    }

    public string GetDeadLetterExchangeName(Type eventType)
    {
        return $"{GetExchangeName(eventType)}.dead_letters";
    }

    public string GetQueueName(Type eventType)
    {
        var serviceName = options.Value.ServiceName;
        Debug.Assert(!string.IsNullOrWhiteSpace(serviceName), "Service Name must be provided");
        var name = $"{options.Value.ServiceName}.{eventType.FullName!.Underscore()}";
        return name;
    }

    public string GetDeadLetterQueueName(Type eventType)
    {
        return $"{GetQueueName(eventType)}.dead_letters";
    }
}

