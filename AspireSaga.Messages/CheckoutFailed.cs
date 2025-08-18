using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record CheckoutFailed([property: Key(0)] Guid CorrelationId) : ISagaMessage;
