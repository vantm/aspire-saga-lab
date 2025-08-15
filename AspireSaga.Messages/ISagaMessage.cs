namespace AspireSaga.Messages;

public interface IMessage;

public interface ISagaMessage : IMessage
{
    Guid CorrelationId { get; }
}
