namespace AspireSaga.Payment;

public record Payment(Guid Id, decimal Price, Guid CorrelationId, long Timestamp);
