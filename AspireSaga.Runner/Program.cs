using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;
using AspireSaga.Runner;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SagaService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq");

builder.Services.AddHostedService<SagaRunner>();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Saga Runner is running");

app.MapGet("/jobs", (SagaService sagaService) =>
{
    var sagas = sagaService.GetSagas();
    return Results.Ok(sagas);
});

app.MapEvent<CheckoutStarted>((evt, sp, token) =>
{
    var svc = sp.GetRequiredService<SagaService>();
    var source = sp.GetRequiredService<ActivitySource>();

    using var activity = source.StartActivity("CheckoutStarted");

    activity?.SetTag("event.correlationId", evt.CorrelationId);
    activity?.SetTag("event.products", evt.Products);

    svc.Save(CheckoutSaga.State.Initial.ToString(), new CheckoutSagaInstance()
    {
        CorrelationId = evt.CorrelationId,
        Products = evt.Products,
    });

    return Task.CompletedTask;
});

app.Run();
