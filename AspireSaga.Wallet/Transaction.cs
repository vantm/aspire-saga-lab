namespace AspireSaga.Wallet;

public record Transaction(Guid Id, decimal Amount, string Note, Guid CorrelationId, long Timestamp);
