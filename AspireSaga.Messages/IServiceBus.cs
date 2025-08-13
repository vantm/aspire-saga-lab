namespace AspireSaga.Messages;

public interface IServiceBus
{
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
