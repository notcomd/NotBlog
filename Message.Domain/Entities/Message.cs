namespace Message.Domain.Entities;

public record Message
{
    public MessageType MessageType { get; set; }

    public object? MessageBody { get; set; }

    public DateTimeOffset MessagePushTime { get; init; } = DateTimeOffset.UtcNow;
}