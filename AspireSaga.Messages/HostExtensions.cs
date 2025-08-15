using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspireSaga.Messages;

public static class HostExtensions
{
    public static IHost MapEvent<TEvent>(this IHost app, Func<TEvent, IServiceProvider, CancellationToken, Task> asyncAction)
    {
        var serviceBus = app.Services.GetRequiredService<IServiceBus>();

        var subId = serviceBus.SubscribeDelegate<TEvent>(async (e, t) =>
        {
            await using var scope = app.Services.CreateAsyncScope();

            var provider = scope.ServiceProvider;

            await asyncAction(e, provider, t);
        });

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStopping.Register(() =>
        {
            serviceBus.Unsubscribe(subId);
        });

        return app;
    }
}
