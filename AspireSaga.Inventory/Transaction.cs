using System.Diagnostics.CodeAnalysis;

namespace AspireSaga.Inventory;

public class Transaction(Guid id, long timestamp, Guid? correlationId, int productId, int change, string note) : IEqualityComparer<Transaction>
{
    public Guid Id { get; } = id;
    public long Timestamp { get; } = timestamp;
    public Guid? CorrelationId { get; } = correlationId;
    public int ProductId { get; } = productId;
    public int Change { get; } = change;
    public string Note { get; } = note;

    public bool Equals(Transaction? x, Transaction? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;
        return x.Id == y.Id;
    }

    public int GetHashCode([DisallowNull] Transaction obj) => obj.Id.GetHashCode();
}
