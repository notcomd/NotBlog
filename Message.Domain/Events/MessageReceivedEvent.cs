
namespace Message.Domain.Events;

/// <summary>消息送达事件。</summary>
public record MessageReceivedEvent(
    Guid MessageId,
    Guid ReceiverId,
    DateTime ReceivedTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}