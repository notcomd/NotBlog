
namespace Message.Domain.Events;

/// <summary>消息转发事件。</summary>
public record MessageForwardedEvent(
    Guid OriginalMessageId,
    Guid ForwardedMessageId,
    Guid ForwardedBy,
    Guid TargetSessionId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}