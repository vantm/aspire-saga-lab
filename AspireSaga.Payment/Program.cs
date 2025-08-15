using AspireSaga.Messages;
using AspireSaga.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaymentService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq");

builder.AddServiceDefaults();

var app = builder.Build();

var sb = app.Services.GetRequiredService<IServiceBus>();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Payment Service is running");

app.MapGet("/payments/{id:guid}", static (Guid id, PaymentService service) => service.Get(id));

app.MapPost("/payments/{id:guid}/complete", static (Guid id, PaymentService service) =>
{
    var payment = service.Get(id);
    if (payment == null)
    {
        return Results.NotFound();
    }
    service.Complete(id);
    return Results.NoContent();
});

app.MapEvent<PurchaseRequest>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<PaymentService>();
    return svc.PayAsync(evt.Price, evt.CorrelationId);
});

app.Run();
