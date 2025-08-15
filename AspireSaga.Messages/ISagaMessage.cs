namespace AspireSaga.Messages;

public interface ISagaMessage : IMessage
{
    Guid CorrelationId { get; }
}
