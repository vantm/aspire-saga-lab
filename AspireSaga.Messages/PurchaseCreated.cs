using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PurchaseCreated(
    [property: Key(0)] Guid CorrelationId) : ISagaMessage;
