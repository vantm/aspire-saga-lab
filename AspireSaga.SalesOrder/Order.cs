namespace AspireSaga.SalesOrder;

public record Order(Guid Id, DateTimeOffset OrderDate, OrderStatus Status, decimal Price, OrderLine[] Lines, Guid CorrelationId);

