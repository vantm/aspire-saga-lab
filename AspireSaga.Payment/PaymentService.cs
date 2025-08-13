using System.Diagnostics;

namespace AspireSaga.Payment;

public class PaymentService
{
    private readonly List<Payment> _payments = [];
    private readonly List<Refunding> _refundings = [];

    public void Pay(decimal value, Guid correlationId)
    {
        Debug.Assert(value > 0, "The payment value must be greater than zero.");

        if (IsPaid(correlationId))
        {
            throw new Exception("The payment has been completed.");
        }

        var payment = new Payment(Guid.NewGuid(), value, correlationId, TimeProvider.System.GetTimestamp());

        _payments.Add(payment);

        //sb.Publish(new Events.PurchaseCompleted(correlationId));
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

    private bool IsPaid(Guid correlationId)
    {
        return _payments.Exists(x => x.CorrelationId == correlationId);
    }
}
