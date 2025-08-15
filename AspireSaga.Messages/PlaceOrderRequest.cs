using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PlaceOrderRequest(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] PlacedProduct[] Products) : ISagaMessage;
