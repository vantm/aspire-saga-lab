namespace AspireSaga.Inventory;

public class TransactionBuilder(TimeProvider time)
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly long _timestamp = time.GetTimestamp();
    private Guid? _correlationId = Guid.Empty;
    private int _productId;
    private int _change;
    private string _note = string.Empty;

    public TransactionBuilder WithNoCorrelationId()
    {
        _correlationId = null;
        return this;
    }

    public TransactionBuilder WithCorrelationId(Guid correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public TransactionBuilder WithProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    public TransactionBuilder WithChange(int change)
    {
        _change = change;
        return this;
    }

    public TransactionBuilder WithNote(string note)
    {
        _note = note;
        return this;
    }

    public Transaction Build()
    {
        if (_timestamp == 0)
        {
            throw new InvalidOperationException("Timestamp must be set before building the transaction.");
        }
        if (_correlationId == Guid.Empty)
        {
            throw new InvalidOperationException("CorrelationId must be set before building the transaction.");
        }
        if (_productId <= 0)
        {
            throw new InvalidOperationException("ProductId must be a positive integer.");
        }
        if (_change == 0)
        {
            throw new InvalidOperationException("Change must be a non-zero integer.");
        }
        return new Transaction(_id, _timestamp, _correlationId, _productId, _change, _note);
    }
}
