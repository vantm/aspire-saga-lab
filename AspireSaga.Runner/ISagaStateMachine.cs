namespace AspireSaga.Runner;

public interface ISagaStateMachine
{
    Guid CorrelationId { get; }
    Task StartAsync();
    Task StopAsync();
    bool IsFinished();
}
