using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record PlacedProduct(
    [property: Key(0)] int ProductId,
    [property: Key(1)] int Quantity);
