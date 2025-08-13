namespace AspireSaga.Inventory;

public class InventoryService(Func<TransactionBuilder> newTxBuilder)
{
    private readonly HashSet<Transaction> _transactions = [];
    private Dictionary<int, int>? _snapshot = null;

    public Dictionary<int, int> GetQuantity()
    {
        if (_snapshot is not null)
        {
            return _snapshot;
        }

        var quantities = new Dictionary<int, int>();

        foreach (var tx in _transactions)
        {
            if (!quantities.ContainsKey(tx.ProductId))
            {
                quantities[tx.ProductId] = 0;
            }
            quantities[tx.ProductId] += tx.Change;
        }

        return quantities;
    }

    public void Import(int productId, int change, string note, Guid correlationId)
    {
        var tx = newTxBuilder()
            .WithCorrelationId(correlationId)
            .WithProductId(productId)
            .WithChange(change)
            .WithNote(note)
            .Build();

        _transactions.Add(tx);

        _snapshot = null;
    }

    public void Export(int productId, int change, string note, Guid correlationId)
    {
        var tx = newTxBuilder()
            .WithCorrelationId(correlationId)
            .WithProductId(productId)
            .WithChange(-change)
            .WithNote(note)
            .Build();

        _transactions.Add(tx);

        _snapshot = null;
    }
}
