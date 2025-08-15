using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PaymentRejected(
    [property: Key(0)] Guid CorrelationId) : ISagaMessage;
