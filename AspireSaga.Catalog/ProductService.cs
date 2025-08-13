namespace AspireSaga.Catalog;

public class ProductService
{
    private readonly IList<Product> _products =
    [
        new Product(1, "Apple", 0.60m),
        new Product(2, "Banana", 0.50m),
        new Product(3, "Cherry", 0.75m),
        new Product(4, "Date", 1.00m),
        new Product(5, "Elderberry", 1.50m),
        new Product(6, "Fig", 0.80m),
        new Product(7, "Grape", 0.90m),
        new Product(8, "Honeydew", 1.20m),
        new Product(9, "Kiwi", 1.10m),
    ];

    // get products
    public IEnumerable<Product> GetProducts()
    {
        return _products;
    }

    // get product by id
    public Product? GetProduct(int id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }
}