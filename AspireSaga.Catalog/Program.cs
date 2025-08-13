using AspireSaga.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<ProductService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Product Service is running");

app.MapGet("/products", static (ProductService service) => service.GetProducts());

app.MapGet("/products/{id:int:min(1)}", static (int id, ProductService service) =>
{
    var product = service.GetProduct(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

await app.RunAsync();
