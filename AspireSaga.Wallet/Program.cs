using AspireSaga.Messages;
using AspireSaga.Wallet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WalletService>();

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

app.MapGet("/", () => "Wallet Service is running");

app.MapGet("/balance", (WalletService service) => service.GetBalance());

app.MapGet("/transactions", (WalletService service) => service.GetTransactions());

app.MapPost("/deposit", (DepositHttpRequest request, WalletService service) =>
{
    service.Deposit(request.Value, request.CorrelationId);
    return Results.Ok();
});

app.MapPost("/withdraw", async (WithdrawHttpRequest request, WalletService service) =>
{
    try
    {
        await service.Withdraw(request.Value, request.CorrelationId);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapEvent<WithdrawRequest>(static (evt, sp, ct) =>
{
    var svc = sp.GetRequiredService<WalletService>();
    return svc.Withdraw(evt.Price, evt.CorrelationId);
});

app.Run();
