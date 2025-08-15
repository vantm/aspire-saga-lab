using MessagePack;

namespace AspireSaga.Messages;

public static class GetProduct
{
    [MessagePackObject]
    public record Request([property: Key(0)] int Id) : IRequest;

    [MessagePackObject]
    public record Reply([property: Key(0)] ProductInformation? Value) : IReply
    {
        public bool IsNone() { return Value is null; }
    }
}

public static class GetProducts
{
    [MessagePackObject]
    public record Request([property: Key(0)] int[] Ids) : IRequest;

    [MessagePackObject]
    public record Reply([property: Key(0)] ProductInformation?[] Value) : IReply;
}
