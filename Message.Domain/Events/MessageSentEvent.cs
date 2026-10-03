
namespace Message.Domain.Events;

/// <summary>消息发送事件。</summary>
public record MessageSentEvent(
    Guid MessageId,
    Guid SenderId,
    Guid? ReceiverId,
    Guid SessionId,
    MessageType MessageType) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}