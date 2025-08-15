using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record OrderDelivered(
    [property: Key(0)] Guid CorrelationId) : ISagaMessage;
