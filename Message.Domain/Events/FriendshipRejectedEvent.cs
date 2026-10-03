
namespace Message.Domain.Events;

/// <summary>好友请求被拒绝事件。</summary>
public record FriendshipRejectedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
