namespace AspireSaga.Runner;

public class SagaStateMachineService
{
    private readonly Dictionary<Guid, ISagaStateMachine> _stateMachines = [];

    public void Save(ISagaStateMachine stateMachine)
    {
        var correlationId = stateMachine.CorrelationId;
        if (!_stateMachines.TryAdd(correlationId, stateMachine))
        {
            _stateMachines[correlationId] = stateMachine;
        }
    }

    public IEnumerable<ISagaStateMachine> All()
    {
        return [.. _stateMachines.Values];
    }
}
