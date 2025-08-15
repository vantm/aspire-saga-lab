using AspireSaga.Messages;
using System.Diagnostics;

namespace AspireSaga.Runner;

public class SagaRunner(SagaStateMachineService sagas, ILogger<SagaRunner> logger) : BackgroundService
{
    private readonly Dictionary<Guid, Task> _runningSagas = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var newSagas = sagas.All()
                .Where(x => !x.IsFinished())
                .Where(x => !_runningSagas.ContainsKey(x.CorrelationId));

            foreach (var saga in newSagas)
            {
                _runningSagas.Add(saga.CorrelationId, StartSagaAsync(saga, stoppingToken));
            }

            await Task.Delay(1000, stoppingToken);
        }

        await Task.WhenAll(_runningSagas.Values);

        _runningSagas.Clear();
    }

    private async Task StartSagaAsync(ISagaStateMachine saga, CancellationToken cancellationToken)
    {
        var correlationId = saga.CorrelationId;
        var activity = Activity.Current?.Source.StartActivity("StartSaga");

        activity?.SetTag("saga.correlationId", correlationId);
        activity?.SetTag("saga.type", saga.GetType().FullName);

        logger.LogInformation("Executing the saga {CorrelationId}...", correlationId);

        try
        {

            await saga.StartAsync();

            while (!saga.IsFinished())
            {
                await Task.Delay(1000, cancellationToken);
            }

            await saga.StopAsync();

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saga {CorrelationId} failed", correlationId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _runningSagas.Remove(correlationId); // remove to retry
        }
        finally
        {
            activity?.Stop();
        }
    }
}
