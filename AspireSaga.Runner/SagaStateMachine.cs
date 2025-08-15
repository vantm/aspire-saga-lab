using AspireSaga.Messages;
using Stateless;
using System.Diagnostics;

namespace AspireSaga.Runner;

public abstract class SagaStateMachine<TSagaInstance, TState, TTrigger> : ISagaStateMachine
    where TSagaInstance : ISagaInstance
{
    protected StateMachine<TState, TTrigger> _stateMachine;
    protected TSagaInstance _saga;

    protected SagaStateMachine()
    {
        _saga = GetInitialSagaState()!;
        _stateMachine = new StateMachine<TState, TTrigger>(GetInitialStateValue()!);
        DefineStateMachine(_stateMachine);
    }

    protected TSagaInstance Saga => _saga;

    protected abstract void DefineStateMachine(StateMachine<TState, TTrigger> stateMachine);
    protected abstract TSagaInstance GetInitialSagaState();
    protected abstract TState GetInitialStateValue();
    protected abstract IServiceBus GetServiceBus();

    protected ActivitySource? ActivitySource { get; private set; }

    private readonly List<Guid> subscription = [];

    public Guid CorrelationId => _saga.CorrelationId;

    protected void When<TEvt>(TTrigger trigger, Action<InvalidOperationException>? onFailed = null)
    {
        var subId = GetServiceBus().SubscribeDelegate<TEvt>(async (evt, token) =>
        {
            ActivitySource = Activity.Current?.Source;

            var activity = ActivitySource?.StartActivity("SagaStateMachine.When", ActivityKind.Consumer);

            try
            {
                activity?.SetTag("saga.type", GetType().FullName);
                activity?.SetTag("saga.trigger", trigger);

                await _stateMachine.FireAsync(trigger);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (InvalidOperationException ex)
            {
                onFailed?.Invoke(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
            finally
            {
                activity?.Stop();
                activity?.Dispose();
            }
        });

        subscription.Add(subId);
    }

    protected virtual void BeforeActivate()
    {
    }

    protected virtual void AfterActivate()
    {
    }

    public async Task StartAsync()
    {
        var activity = ActivitySource?.StartActivity("SagaStateMachine.ActivateAsync", ActivityKind.Consumer);

        try
        {
            BeforeActivate();

            activity?.SetTag("saga.state-machine.state", _stateMachine.State);

            await _stateMachine.ActivateAsync();

            AfterActivate();

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            activity?.Stop();
            activity?.Dispose();
        }
    }

    public virtual Task StopAsync()
    {
        var sb = GetServiceBus();
        foreach (var subId in subscription)
        {
            sb.Unsubscribe(subId);
        }

        return Task.CompletedTask;
    }

    public abstract bool IsFinished();
}
