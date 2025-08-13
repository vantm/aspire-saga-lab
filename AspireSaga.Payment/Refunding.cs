namespace AspireSaga.Payment;

public record Refunding(Guid Id, decimal Price, Guid CorrelationId, long Timestamp);
