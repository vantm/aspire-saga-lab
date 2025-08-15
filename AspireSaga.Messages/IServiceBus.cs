namespace AspireSaga.Messages;

public interface IServiceBus
{
    Guid Subscribe<TEvent>(Func<IServiceProvider, IConsumer<TEvent>> factory);
    void Unsubscribe(Guid subscriptionId);
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
