namespace AspireSaga.Wallet;

public record WithdrawHttpRequest(decimal Value, Guid CorrelationId);
