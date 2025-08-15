using AspireSaga.Messages;

namespace AspireSaga.Runner;

public class CheckoutSagaInstance : ISagaInstance
{
    public required Guid CorrelationId { get; init; }
    public required PlacedProduct[] Products { get; init; }

    public decimal Price { get; set; } = 777;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailedReason { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? Address { get; set; } = "Default Address";
}
