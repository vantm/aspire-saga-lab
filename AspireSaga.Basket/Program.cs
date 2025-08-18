using AspireSaga.Basket;
using AspireSaga.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BasketService>();

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

app.MapGet("/", () => "Basket Service is running");

app.MapGet("/baskets", static (BasketService service) => service.GetItems());

app.MapGet("/checkouts", static (BasketService service) => service.GetCheckouts());

app.MapPost("/checkouts", static async (IServiceBus bus, BasketService svc, CancellationToken token) =>
{
    var correlationId = Guid.NewGuid();
    await svc.CheckoutAsync(correlationId, token);
    return Results.Accepted();
});

app.MapPost("/baskets", static (UpdateBasketItemRequest[] body, BasketService service) =>
{
    foreach (var item in body)
    {
        if (item.Quantity <= 0)
        {
            return Results.BadRequest("Quantity must be greater than zero.");
        }
        if (item.ProductId <= 0)
        {
            return Results.BadRequest("ProductId must be a positive integer.");
        }
    }

    foreach (var item in body)
    {
        service.UpdateItem(item.ProductId, item.Quantity);
    }

    return Results.NoContent();
});

app.MapEvent<CheckoutFailed>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<BasketService>();
    svc.HandleFailedCheckout(evt.CorrelationId);
    return Task.CompletedTask;
});

app.MapEvent<CheckoutAccepted>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<BasketService>();
    svc.HandleAcceptedCheckout(evt.CorrelationId);
    return Task.CompletedTask;
});

app.Run();
