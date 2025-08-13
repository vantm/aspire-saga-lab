using AspireSaga.Inventory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Func<TransactionBuilder>>((IServiceProvider sp) =>
{
    var timeProvider = sp.GetRequiredService<TimeProvider>();
    return () => new TransactionBuilder(timeProvider);
});
builder.Services.AddSingleton<InventoryService>();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Inventory Service is running");

app.MapGet("/stocks", static (InventoryService service) => service.GetQuantity());

app.Run();
