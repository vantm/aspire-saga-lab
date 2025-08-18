using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record CheckoutAccepted([property: Key(0)] Guid CorrelationId) : ISagaMessage;
