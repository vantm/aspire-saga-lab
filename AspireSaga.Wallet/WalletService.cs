namespace AspireSaga.Wallet;

public class WalletService
{
    private readonly List<Transaction> _transactions = [];

    public decimal GetBalance()
    {
        return _transactions.Sum(x => x.Amount);
    }

    public IEnumerable<Transaction> GetTransactions()
    {
        const int LIMIT = 50;
        if (_transactions.Count > LIMIT)
        {
            return [];
        }
        return _transactions[^LIMIT..];
    }

    public void Deposit(decimal value, Guid correlationId)
    {
        var tx = new Transaction(Guid.NewGuid(), value, "Deposit", correlationId, TimeProvider.System.GetTimestamp());
        _transactions.Add(tx);
    }

    public void Withdraw(decimal value, Guid correlationId)
    {
        if (GetBalance() < value)
        {
            throw new Exception("Insufficient Balance");
        }

        var tx = new Transaction(Guid.NewGuid(), -value, "Withdraw", correlationId, TimeProvider.System.GetTimestamp());
        _transactions.Add(tx);
    }
}
