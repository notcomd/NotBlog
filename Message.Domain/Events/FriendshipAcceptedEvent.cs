
namespace Message.Domain.Events;

/// <summary>好友请求被接受事件。</summary>
public record FriendshipAcceptedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}