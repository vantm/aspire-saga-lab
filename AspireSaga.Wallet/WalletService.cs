using AspireSaga.Messages;

namespace AspireSaga.Wallet;

public class WalletService(IServiceBus sb)
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
            return _transactions[^LIMIT..];
        }
        return _transactions.AsReadOnly();
    }

    public void Deposit(decimal value, Guid correlationId)
    {
        var tx = new Transaction(Guid.NewGuid(), value, "Deposit", correlationId, TimeProvider.System.GetTimestamp());
        _transactions.Add(tx);
    }

    public async Task Withdraw(decimal value, Guid correlationId)
    {
        if (GetBalance() < value)
        {
            await sb.PublishAsync(new WithdrawRejected(correlationId, "Insufficient Balance"));
            throw new Exception("Insufficient balance for withdrawal.");
        }

        var tx = new Transaction(Guid.NewGuid(), -value, "Withdraw", correlationId, TimeProvider.System.GetTimestamp());

        _transactions.Add(tx);

        await sb.PublishAsync(new WithdrawCompleted(correlationId, value));
    }
}
