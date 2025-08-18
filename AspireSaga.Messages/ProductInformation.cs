using MessagePack;

namespace AspireSaga.Messages;

[MessagePackObject]
public record ProductInformation(
    [property: Key(0)] int Id,
    [property: Key(1)] string Name,
    [property: Key(2)] decimal Price);
