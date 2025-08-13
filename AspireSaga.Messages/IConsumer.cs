namespace AspireSaga.Messages;

public interface IConsumer<in T>
{
    Task ConsumeAsync(T message, CancellationToken cancellationToken);
}
