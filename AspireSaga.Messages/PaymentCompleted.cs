using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PaymentCompleted(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] DateTimeOffset? CompletedAt) : ISagaMessage;
