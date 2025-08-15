using AspireSaga.Messages;
using Stateless;

namespace AspireSaga.Runner;

public class CheckoutSaga(Guid correlationId, IServiceBus sb, ILogger<CheckoutSaga> logger) : SagaStateMachine<CheckoutSagaInstance, CheckoutSaga.State, CheckoutSaga.Trigger>
{
    public enum State
    {
        Initial, // wait for placing the order
        Pending, // wait for creating the payment
        PaymentProcessing, // wait for user to pay the order
        PaymentFailed, // user rejected the payment
        Accepted, // user completed the payment
        Completed, // the order is delivered
    }

    public enum Trigger
    {
        PlaceOrder,
        CreatePayment,
        RejectPayment,
        CompletePayment,
        DeliverOrder,
    }

    protected override void DefineStateMachine(StateMachine<State, Trigger> stateMachine)
    {
        Task PublishPlaceOrderRequest()
        {
            return sb.PublishAsync(new PlaceOrderRequest(Saga.CorrelationId, Saga.Products));
        }
        void UpdateStateOnExitInit()
        {
            _saga.CreatedAt = DateTimeOffset.Now;
        }
        stateMachine.Configure(State.Initial)
            .Permit(Trigger.PlaceOrder, State.Pending)
            .OnActivateAsync(PublishPlaceOrderRequest, nameof(PublishPlaceOrderRequest))
            .OnExit(UpdateStateOnExitInit, nameof(UpdateStateOnExitInit));

        Task PublishCreatePaymentRequest()
        {
            return sb.PublishAsync(new PurchaseRequest(Saga.CorrelationId, Saga.Price));
        }
        stateMachine.Configure(State.Pending)
            .Permit(Trigger.CreatePayment, State.PaymentProcessing)
            .OnEntryAsync(PublishCreatePaymentRequest, nameof(PublishCreatePaymentRequest))
            .OnActivateAsync(PublishCreatePaymentRequest, nameof(PublishCreatePaymentRequest));

        stateMachine.Configure(State.PaymentProcessing)
            .Permit(Trigger.CompletePayment, State.Accepted)
            .Permit(Trigger.RejectPayment, State.PaymentFailed);

        void UpdateStateOnPaymentFailed()
        {
            _saga.FailedAt = DateTimeOffset.Now; // TODO: from event
            _saga.FailedReason = nameof(State.PaymentFailed);
        }
        stateMachine.Configure(State.PaymentFailed)
            .OnEntry(UpdateStateOnPaymentFailed, nameof(UpdateStateOnPaymentFailed));

        Task PublishDeliverOrderRequest()
        {
            return sb.PublishAsync(new DeliverOrderRequest(Saga.CorrelationId, Saga.Products, Saga.Address!));
        }
        void UpdateStateOnAccepted()
        {
            _saga.PaidAt = DateTimeOffset.Now;
        }
        stateMachine.Configure(State.Accepted)
            .Permit(Trigger.DeliverOrder, State.Completed)
            .OnActivateAsync(PublishDeliverOrderRequest, nameof(PublishDeliverOrderRequest))
            .OnEntryAsync(PublishDeliverOrderRequest, nameof(PublishDeliverOrderRequest))
            .OnEntry(UpdateStateOnAccepted, nameof(UpdateStateOnAccepted));

        void UpdateStateOnCompleted()
        {
            _saga.DeliveredAt = DateTimeOffset.Now;
        }
        stateMachine.Configure(State.Completed)
            .OnEntry(UpdateStateOnCompleted, nameof(UpdateStateOnCompleted));

        stateMachine.OnTransitionCompleted(t =>
        {
            logger.LogInformation("Transitioned: {Source} -> {Destination}", t.Source, t.Destination);
        });
    }

    protected override CheckoutSagaInstance GetInitialSagaState()
    {
        return new() { CorrelationId = correlationId, Products = [] };
    }

    protected override State GetInitialStateValue()
    {
        return State.Initial;
    }

    public override bool IsFinished()
    {
        return _stateMachine.IsInState(State.Completed) ||
               _stateMachine.IsInState(State.PaymentFailed);
    }

    protected override void BeforeActivate()
    {
        When<OrderPlaced>(Trigger.PlaceOrder);
        When<PurchaseCreated>(Trigger.CreatePayment);
        When<PaymentRejected>(Trigger.RejectPayment);
        When<PaymentCompleted>(Trigger.CompletePayment);
        When<OrderDelivered>(Trigger.DeliverOrder);
    }

    protected override IServiceBus GetServiceBus()
    {
        return sb;
    }
}
