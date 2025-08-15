namespace AspireSaga.Runner;

public class SagaService
{
    private readonly Dictionary<Guid, (string, ISagaInstance)> _sagas = [];

    public void Save(string state, ISagaInstance saga)
    {
        if (!_sagas.TryAdd(saga.CorrelationId, (state, saga)))
        {
            _sagas[saga.CorrelationId] = (state, saga);
        }
    }

    public (string, ISagaInstance)? Get(Guid correlationId)
    {
        if (_sagas.TryGetValue(correlationId, out var saga))
        {
            return saga;
        }
        return null;
    }

    public IEnumerable<Guid> GetCorrelationIds()
    {
        return [.. _sagas.Keys];
    }

    public IEnumerable<ISagaInstance> GetSagas()
    {
        return [.. _sagas.Values.Select(x => x.Item2)];
    }
}
