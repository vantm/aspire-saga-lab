namespace AspireSaga.Messages.RabbitMQ;

public class RabbitMqOptions
{
    public string ServiceName { get; set; } = string.Empty;
    public HashSet<Type> EventTypes { get; } = [];
}
