using AspireSaga.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaymentService>();

builder.AddServiceDefaults();
var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
