namespace AspireSaga.Basket;

public record Checkout(Guid Id, BasketItem[] Items, CheckoutStatus Status, Guid CorrelationId, long Timestamp);
