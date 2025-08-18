using AspireSaga.Messages;
using AspireSaga.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaymentService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq", f =>
{
    f.DisableTracing = true;
});

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Payment Service is running");

app.MapGet("/payments/{id:guid}", static (Guid id, PaymentService service) => service.Get(id));

app.MapPost("/payments/{id:guid}/complete", static async (Guid id, PaymentService service) =>
{
    var payment = service.Get(id);
    if (payment == null)
    {
        return Results.NotFound();
    }

    await service.Complete(id);

    return Results.NoContent();
});

app.MapEvent<PaymentRequest>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<PaymentService>();
    return svc.PayAsync(evt.Price, evt.CorrelationId);
});

app.MapEvent<PaymentCreated>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<PaymentService>();
    var sb = sp.GetRequiredService<IServiceBus>();
    var payment = svc.Get(evt.CorrelationId);

    if (payment == null)
    {
        throw new Exception("Payment not found.");
    }

    var withdrawEvent = new WithdrawRequest(payment.CorrelationId, payment.Price);

    return sb.PublishAsync(withdrawEvent, ct);
});

app.MapEvent<WithdrawCompleted>(static async (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<PaymentService>();
    await svc.Complete(evt.CorrelationId);
});

app.MapEvent<WithdrawRejected>(static async (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<PaymentService>();
    await svc.Reject(evt.CorrelationId, evt.Error);
});

app.Run();
