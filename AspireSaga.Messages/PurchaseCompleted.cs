using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PurchaseCompleted(
    [property: Key(0)] Guid CorrelationId);
