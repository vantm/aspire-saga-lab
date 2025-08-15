using AspireSaga.Messages;
using System.Diagnostics;

namespace AspireSaga.Runner;

public class SagaRunner(SagaService sagas, IServiceBus sb, ILoggerFactory loggerFactory, ILogger<SagaRunner> logger) : BackgroundService
{
    private readonly Dictionary<Guid, Task> _runningSagas = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var correlationId in sagas.GetCorrelationIds())
            {
                if (_runningSagas.ContainsKey(correlationId))
                {
                    continue;
                }

                var task = Task.Run(async () =>
                {
                    var activity = Activity.Current?.Source.StartActivity("StartSaga");

                    logger.LogInformation("Executing the saga {CorrelationId}...", correlationId);

                    try
                    {
                        var saga = new CheckoutSaga(correlationId, sagas, sb, loggerFactory.CreateLogger<CheckoutSaga>());

                        activity?.SetTag("saga.correlationId", correlationId);

                        await saga.ActivateAsync();

                        while (!saga.IsFinished())
                        {
                            await Task.Delay(500, stoppingToken);
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
                }, stoppingToken);

                _runningSagas.Add(correlationId, task);
            }

            await Task.Delay(1000, stoppingToken); // Polling interval
        }

        await Task.WhenAll(_runningSagas.Values);

        _runningSagas.Clear();
    }
}
