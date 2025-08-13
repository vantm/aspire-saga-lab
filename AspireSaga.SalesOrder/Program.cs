using AspireSaga.Messages;
using AspireSaga.Messages.RabbitMQ;
using AspireSaga.SalesOrder;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAspireSagaMessaging()
    .AddAllEvents()
    .AddConsumer<CheckoutConsumer, CheckoutStarted>()
    .Configure<RabbitMqOptions>(opt =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging-rabbit-mq")!;
        opt.Uri = new Uri(connectionString);
    });

builder.AddServiceDefaults();

builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", static () => "Sales Order Service is running");

app.MapGet("/orders", static (OrderService service) => service.GetOrders());

app.Run();
