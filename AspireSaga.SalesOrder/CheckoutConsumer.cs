using AspireSaga.Messages;

namespace AspireSaga.SalesOrder;

public class CheckoutConsumer(OrderService service, TimeProvider time, ILogger<CheckoutConsumer> logger) : IConsumer<CheckoutStarted>
{
    public Task ConsumeAsync(CheckoutStarted message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received a checkout: {CorrelationId}", message.CorrelationId);

        var lines = message.Products
            .Select(p => new OrderLine(p.ProductId, p.Quantity))
            .ToArray();

        return service.PlaceOrderAsync(time.GetLocalNow(), 100, lines, message.CorrelationId);
    }
}
