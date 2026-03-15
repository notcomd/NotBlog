using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

public class Message : Entity, IAggregateRoot
{
    public MessageType MessageType { get; set; }

    public object? MessageBody { get; set; }

    public DateTimeOffset MessagePushTime { get; init; } = DateTimeOffset.UtcNow;
}