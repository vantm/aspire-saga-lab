using Humanizer;
using RabbitMQ.Client;

namespace AspireSaga.Messages.RabbitMQ;

class RabbitMqInstances
{
    public IConnection Connection { get; set; } = default!;
    public IChannel ServiceBusChannel { get; set; } = default!;
    public IChannel MessageConsumerChannel { get; set; } = default!;

    public string GetQueueOrExchangeName(Type eventType)
    {
        return eventType.FullName!.Underscore();
    }
}

