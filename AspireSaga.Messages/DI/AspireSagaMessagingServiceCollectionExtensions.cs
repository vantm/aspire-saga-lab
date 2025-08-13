using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class AspireSagaMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddAspireSagaMessaging(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMqManager>();

        services.AddSingleton<IServiceBus, RabbitMqServiceBus>();
        services.AddSingleton<RabbitMqInstances>();

        services.AddOptions<RabbitMqOptions>();

        return services;
    }

    public static IServiceCollection AddConsumer<T, E>(this IServiceCollection services)
        where T : class, IConsumer<E>
    {
        services.AddTransient<IConsumer<E>, T>();
        services.AddHostedService<RabbitMqWorker<T, E>>();
        return services;
    }

    public static IServiceCollection AddEvent<T>(this IServiceCollection services)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            options.EventTypes.Add(typeof(T));
        });

        return services;
    }

    public static IServiceCollection AddAllEvents(this IServiceCollection services)
    {
        return services.AddEvent<CheckoutStarted>()
                       .AddEvent<PlaceOrderRequest>()
                       .AddEvent<OrderPlaced>()
                       .AddEvent<PurchaseRequest>()
                       .AddEvent<PurchaseCompleted>();
    }
}
