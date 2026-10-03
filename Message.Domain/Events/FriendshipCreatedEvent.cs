
namespace Message.Domain.Events;

/// <summary>好友关系建立事件（发起好友请求）。</summary>
public record FriendshipCreatedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}