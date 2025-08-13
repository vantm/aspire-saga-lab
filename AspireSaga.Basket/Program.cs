using AspireSaga.Basket;
using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BasketService>();

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents()
    .Configure<RabbitMqOptions>(opt =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging-rabbit-mq")!;
        opt.Uri = new Uri(connectionString);
    });

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Basket Service is running");

app.MapGet("/baskets", static (BasketService service) => service.GetItems());

app.MapGet("/checkouts", static (BasketService service) => service.GetCheckouts());

app.MapPost("/checkouts", static async (IServiceBus bus, BasketService service) =>
{
    try
    {
        await service.Checkout();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
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

app.Run();
