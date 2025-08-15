using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;
using Humanizer;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Reflection;

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

        services.AddOptions<RabbitMqOptions>()
            .Configure<IHostEnvironment>((opts, env) =>
            {
                var applicationName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                                      ?? env.ApplicationName
                                      ?? throw new InvalidOperationException("Application name is not set.");

                Console.WriteLine("Application: {0}", applicationName);

                applicationName = applicationName.Humanize().Underscore();

                opts.ServiceName = applicationName;
            });

        return services;
    }

    public static IServiceCollection AddEvent<T>(this IServiceCollection services)
    {
        services.Configure<RabbitMqOptions>(options =>
        {
            options.EventTypes.Add(typeof(T));
        });

        services.AddHostedService<RabbitMqWorker<T>>();

        return services;
    }

    public static IServiceCollection AddAllEvents(this IServiceCollection services)
    {
        var addEventMethod = typeof(AspireSagaMessagingServiceCollectionExtensions)
            .GetMethod(nameof(AddEvent), BindingFlags.Static | BindingFlags.Public)!;

        Debug.Assert(addEventMethod != null, "AddEvent method should not be null.");

        typeof(CheckoutStarted).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IMessage)))
            .ToList()
            .ForEach(type => addEventMethod.MakeGenericMethod(type).Invoke(null, [services]));

        return services;
    }
}
