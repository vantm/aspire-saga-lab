using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record WithdrawCompleted(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] decimal Price) : ISagaMessage;
