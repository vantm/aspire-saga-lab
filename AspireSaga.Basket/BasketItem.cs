namespace AspireSaga.Basket;

public record BasketItem(int ProductId, int Quantity);

public record Checkout(Guid Id, BasketItem[] Items, CheckoutStatus Status, Guid CorrelationId, long Timestamp);

public enum CheckoutStatus
{
    Pending,
    Completed,
    Failed
}
