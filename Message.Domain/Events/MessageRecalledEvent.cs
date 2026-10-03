
namespace Message.Domain.Events;

/// <summary>消息撤回事件。</summary>
public record MessageRecalledEvent(
    Guid MessageId,
    Guid RecalledBy,
    RecallReason Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}