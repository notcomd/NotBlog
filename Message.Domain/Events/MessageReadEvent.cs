
namespace Message.Domain.Events;

/// <summary>消息已读事件。</summary>
public record MessageReadEvent(
    Guid MessageId,
    Guid ReaderId,
    DateTime ReadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}