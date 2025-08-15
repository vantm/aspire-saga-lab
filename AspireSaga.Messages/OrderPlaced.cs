using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record OrderPlaced(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] PlacedProduct[] Products,
    [property: Key(2)] decimal Price) : ISagaMessage;
