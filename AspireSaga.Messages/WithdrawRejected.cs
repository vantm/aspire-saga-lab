using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record WithdrawRejected(
    [property: Key(0)] Guid CorrelationId,
    [property: Key(1)] string Error) : ISagaMessage;
