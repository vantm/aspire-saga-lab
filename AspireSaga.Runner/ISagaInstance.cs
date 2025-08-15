namespace AspireSaga.Runner;

public interface ISagaInstance
{
    Guid CorrelationId { get; }
}
