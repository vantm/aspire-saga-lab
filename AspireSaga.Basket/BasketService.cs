using AspireSaga.Messages;
using System.Diagnostics;

namespace AspireSaga.Basket;

public class BasketService(IServiceBus bus)
{
    // key: ProductId, value: Basket
    private readonly Dictionary<int, BasketItem> _baskets = [];
    private readonly List<Checkout> _checkouts = [];

    public BasketItem[] GetItems() => [.. _baskets.Values];

    public bool HasItem(int productId)
    {
        Debug.Assert(productId > 0, "ProductId must be a positive integer.");
        return _baskets.ContainsKey(productId);
    }

    public void UpdateItem(int productId, int quantity)
    {
        Debug.Assert(productId > 0, "ProductId must be a positive integer.");
        Debug.Assert(quantity >= 0, "Quantity must be greater than zero.");

        if (_baskets.ContainsKey(productId))
        {
            if (quantity == 0)
            {
                // Remove the product from the basket if quantity is zero
                _baskets.Remove(productId);
            }
            else
            {
                _baskets[productId] = new BasketItem(productId, quantity);
            }
        }
        else
        {
            Debug.Assert(quantity > 0, "Quantity must be greater than zero when adding a new product to the basket.");
            _baskets.Add(productId, new BasketItem(productId, quantity));
        }
    }

    public IEnumerable<Checkout> GetCheckouts()
    {
        return _checkouts.AsReadOnly();
    }

    public async Task CheckoutAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("CorrelationId cannot be empty.", nameof(correlationId));
        }
        if (_checkouts.Exists(x => x.CorrelationId == correlationId))
        {
            throw new InvalidOperationException($"Checkout with CorrelationId {correlationId} already exists.");
        }
        if (_baskets.Count == 0)
        {
            throw new Exception("Basket is empty.");
        }

        var items = _baskets.Values.ToArray();
        var checkout = new Checkout(Guid.NewGuid(), items, CheckoutStatus.Pending, correlationId, TimeProvider.System.GetTimestamp());

        _checkouts.Add(checkout);

        // Clear the basket after checkout
        _baskets.Clear();

        var products = items.Select(x => new PlacedProduct(x.ProductId, x.Quantity)).ToArray();
        var @event = new CheckoutStarted(checkout.CorrelationId, products);

        await bus.PublishAsync(@event, cancellationToken);
    }
}
