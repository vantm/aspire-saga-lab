namespace AspireSaga.Delivery;

public record Delivery(Guid Id, Guid CorrelationId, DeliveryItem[] Items, string Address, DateTimeOffset? DeliveredAt);