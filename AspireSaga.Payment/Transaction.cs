namespace AspireSaga.Payment;

public record Payment(Guid Id, decimal Price, PaymentStatus Status, Guid CorrelationId, long Timestamp, DateTimeOffset? CompletedAt, string? ErrorMessage);
