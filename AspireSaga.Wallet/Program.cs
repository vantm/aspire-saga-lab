using AspireSaga.Wallet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WalletService>();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Wallet Service is running");

app.MapGet("/balance", (WalletService service) => service.GetBalance());

app.MapGet("/transactions", (WalletService service) => service.GetTransactions());

app.MapPost("/deposit", (decimal value, Guid correlationId, WalletService service) =>
{
    service.Deposit(value, correlationId);
    return Results.Ok();
});

app.MapPost("/withdraw", (decimal value, Guid correlationId, WalletService service) =>
{
    try
    {
        service.Withdraw(value, correlationId);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();
