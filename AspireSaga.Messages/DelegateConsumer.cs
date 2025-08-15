using System.Diagnostics;

namespace AspireSaga.Messages;

public sealed class DelegateConsumer<T>(Func<T, CancellationToken, Task> action) : IConsumer<T>
{
    public Task ConsumeAsync(T message, CancellationToken cancellationToken)
    {
        Debug.Assert(action is not null, "Action cannot be null.");
        return action(message, cancellationToken);
    }
}
