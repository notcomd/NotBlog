
namespace Message.Domain.Events;

/// <summary>用户取消关注事件。</summary>
public record UserUnfollowedEvent(Guid FollowerGuid,
    Guid FolloweeGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
