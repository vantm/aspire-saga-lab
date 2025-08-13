using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record CheckoutStarted(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] PlacedProduct[] Products);
