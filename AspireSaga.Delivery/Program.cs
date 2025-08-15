using AspireSaga.Delivery;
using AspireSaga.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DeliverService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq");

var app = builder.Build();

app.MapGet("/", () => "Delivery Service is running");

app.MapPost("/deliveries/{id:guid}/packaged", static (Guid id, DeliverService service) =>
{
    service.SetPackaged(id);
    return Results.NoContent();
});

app.MapPost("/deliveries/{id:guid}/delivered", static async (Guid id, DeliverService service) =>
{
    await service.SetDelivered(id);
    return Results.NoContent();
});

app.MapEvent<DeliverOrderRequest>(static (evt, sp, token) =>
{
    var svc = sp.GetRequiredService<DeliverService>();

    var items = evt.Products
        .Select(p => new DeliveryItem(p.ProductId, p.Quantity))
        .ToArray();

    svc.Add(evt.CorrelationId, items, evt.Address);

    return Task.CompletedTask;
});

app.Run();
