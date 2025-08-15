using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;
using AspireSaga.Runner;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SagaStateMachineService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq");

builder.Services.AddHostedService<SagaRunner>();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Saga Runner is running");

app.MapGet("/jobs", (SagaStateMachineService sagas) =>
{
    var data = sagas.All()
        .Where(x => !x.IsFinished())
        .Select(x => new
        {
            x.CorrelationId,
        });
    return Results.Ok(data);
});

app.MapEvent<CheckoutStarted>((evt, sp, token) =>
{
    var sagas = sp.GetRequiredService<SagaStateMachineService>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var sb = sp.GetRequiredService<IServiceBus>();

    var source = sp.GetRequiredService<ActivitySource>();

    using var activity = source.StartActivity("CheckoutStarted");

    activity?.SetTag("event.correlationId", evt.CorrelationId);
    activity?.SetTag("event.products", evt.Products);

    sagas.Save(new CheckoutSaga(evt.CorrelationId, sb, loggerFactory.CreateLogger<CheckoutSaga>()));

    return Task.CompletedTask;
});

app.Run();
