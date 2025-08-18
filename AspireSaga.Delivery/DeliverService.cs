using AspireSaga.Messages;
using System.Diagnostics;

namespace AspireSaga.Delivery;

public class DeliverService(TimeProvider time, IServiceBus bus)
{
    private readonly Dictionary<Guid, Delivery> _items = [];

    public void Add(Guid correlationId, DeliveryItem[] items, string address)
    {
        Debug.Assert(correlationId != Guid.Empty, "CorrelationId cannot be empty.");
        Debug.Assert(items is { Length: > 0 }, "Delivery has no item");
        Debug.Assert(!_items.ContainsKey(correlationId), $"Delivery with CorrelationId {correlationId} already exists.");

        var item = new Delivery(Guid.NewGuid(), correlationId, items, address, null);

        _items.Add(item.CorrelationId, item);
    }

    public Task Complete(Guid correlationId)
    {
        Debug.Assert(correlationId != Guid.Empty, "CorrelationId cannot be empty.");
        Debug.Assert(_items.ContainsKey(correlationId), $"Delivery with CorrelationId {correlationId} does not exist.");

        var item = _items[correlationId];

        Debug.Assert(item.DeliveredAt is null, $"Delivery with CorrelationId {correlationId} is already delivered.");

        _items[correlationId] = item with
        {
            DeliveredAt = time.GetUtcNow(),
        };
        return bus.PublishAsync(new OrderDelivered(item.CorrelationId));
    }
}