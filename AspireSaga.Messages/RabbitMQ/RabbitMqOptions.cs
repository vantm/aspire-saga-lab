namespace AspireSaga.Messages.RabbitMQ;

public class RabbitMqOptions
{
    public Uri Uri { get; set; } = new("amqp://guest:guest@localhost:5672");
    public HashSet<Type> EventTypes { get; } = [];
}
