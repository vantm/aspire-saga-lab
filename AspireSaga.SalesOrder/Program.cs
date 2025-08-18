using AspireSaga.Messages;
using AspireSaga.SalesOrder;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents();

builder.AddRabbitMQClient("messaging-rabbit-mq", f =>
{
    f.DisableTracing = true;
});

builder.AddServiceDefaults();

builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", static () => "Sales Order Service is running");

app.MapGet("/orders", static (OrderService service) => service.GetOrders());

app.MapEvent<PlaceOrderRequest>(static (evt, sp, token) =>
{
    var svc = sp.GetRequiredService<OrderService>();
    var time = sp.GetRequiredService<TimeProvider>();
    var lines = evt.Products
        .Select(p => new OrderLine(p.ProductId, p.Quantity))
        .ToArray();
    return svc.PlaceOrderAsync(time.GetLocalNow(), lines.Length * 100, lines, evt.CorrelationId);
});

app.Run();
