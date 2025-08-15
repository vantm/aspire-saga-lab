using Microsoft.Extensions.DependencyInjection;

namespace AspireSaga.Messages;

public static class ServiceBusExtensions
{
    public static IDisposable SubscribeConsumer<TEvent>(this IServiceBus serviceBus)
    {
        var subscriptionId = serviceBus.Subscribe(sp => sp.GetRequiredService<IConsumer<TEvent>>());
        return new DisposableAction(() => serviceBus.Unsubscribe(subscriptionId));
    }

    public static Guid SubscribeDelegate<TEvent>(this IServiceBus serviceBus, Func<TEvent, CancellationToken, Task> asyncAction)
    {
        return serviceBus.Subscribe(sp => new DelegateConsumer<TEvent>(asyncAction));
    }
}
