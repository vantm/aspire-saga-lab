using AspireSaga.Messages;
using System.Diagnostics;

namespace AspireSaga.Payment;

public class PaymentService(IServiceBus sb, ActivitySource source)
{
    private readonly List<Payment> _payments = [];
    private readonly List<Refunding> _refundings = [];

    public Payment? Get(Guid correlationId)
    {
        Debug.Assert(correlationId != Guid.Empty, "The correlation ID must not be empty.");
        return _payments.Find(x => x.CorrelationId == correlationId);
    }

    public Task PayAsync(decimal value, Guid correlationId)
    {
        Debug.Assert(value > 0, "The payment value must be greater than zero.");

        if (IsPaid(correlationId))
        {
            throw new Exception("The payment has been completed.");
        }

        var payment = new Payment(Guid.NewGuid(), value, PaymentStatus.Pending, correlationId, TimeProvider.System.GetTimestamp(), null, null);

        _payments.Add(payment);

        return sb.PublishAsync(new PaymentCreated(correlationId));
    }

    public void Refund(decimal value, Guid correlationId)
    {
        if (!IsPaid(correlationId))
        {
            throw new Exception("The payment doesn't exist.");
        }

        var refund = new Refunding(Guid.NewGuid(), value, correlationId, TimeProvider.System.GetTimestamp());

        _refundings.Add(refund);
    }

    public async Task Complete(Guid correlationId)
    {
        var activity = source.StartActivity("PaymentService.Complete", ActivityKind.Internal, correlationId.ToString());

        Debug.Assert(correlationId != Guid.Empty, "The correlation ID must be provided.");

        var payment = Get(correlationId);

        if (payment is null)
        {
            throw new Exception("The payment doesn't exist.");
        }

        Debug.Assert(payment.Status == PaymentStatus.Pending, "The payment must be in pending status to complete it.");

        _payments.Remove(payment);

        var completedAt = TimeProvider.System.GetLocalNow();

        activity?.SetTag("completed-at", completedAt.ToString("o"));

        _payments.Add(payment with
        {
            Status = PaymentStatus.Paid,
            CompletedAt = completedAt
        });

        await sb.PublishAsync(new PaymentCompleted(correlationId, completedAt));

        activity?.SetStatus(ActivityStatusCode.Ok);

        activity?.Stop();
    }

    public async Task Reject(Guid correlationId, string reason)
    {
        var activity = source.StartActivity("PaymentService.Reject", ActivityKind.Internal, correlationId.ToString());

        Debug.Assert(correlationId != Guid.Empty, "The correlation ID must be provided.");

        var payment = Get(correlationId);

        if (payment is null)
        {
            throw new Exception("The payment doesn't exist.");
        }

        Debug.Assert(payment.Status == PaymentStatus.Pending, "The payment must be in pending status to complete it.");

        _payments.Remove(payment);

        var rejectedAt = TimeProvider.System.GetLocalNow();

        activity?.SetTag("rejected-at", rejectedAt.ToString("o"));

        _payments.Add(payment with
        {
            Status = PaymentStatus.Failed,
            ErrorMessage = reason
        });

        await sb.PublishAsync(new PaymentRejected(correlationId, reason));

        activity?.SetStatus(ActivityStatusCode.Ok);

        activity?.Stop();
    }

    private bool IsPaid(Guid correlationId)
    {
        return _payments.Exists(x => x.CorrelationId == correlationId);
    }
}
