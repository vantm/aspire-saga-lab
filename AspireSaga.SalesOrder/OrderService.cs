using AspireSaga.Messages;

namespace AspireSaga.SalesOrder;

public class OrderService(IServiceBus bus)
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public Order[] GetOrders()
    {
        return [.. _orders.Values];
    }

    public Order? GetOrder(Guid id)
    {
        return _orders.TryGetValue(id, out var order) ? order : null;
    }

    public async Task<Order> PlaceOrderAsync(DateTimeOffset orderDate, decimal price, OrderLine[] lines, Guid correlationId)
    {
        var order = new Order(Guid.NewGuid(), orderDate, OrderStatus.Pending, price, lines, correlationId);

        _orders.Add(order.Id, order);

        var products = lines.Select(x => new PlacedProduct(x.ProductId, x.Quantity)).ToArray();
        var evt = new OrderPlaced(correlationId, products, price);

        await bus.PublishAsync(evt);

        return order;
    }
}
