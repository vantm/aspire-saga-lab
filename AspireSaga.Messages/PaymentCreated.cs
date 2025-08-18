using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PaymentCreated(
    [property: Key(0)] Guid CorrelationId) : ISagaMessage;
