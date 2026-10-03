
namespace Message.Domain.Events;

/// <summary>用户关注事件。</summary>
public record UserFollowedEvent(Guid FollowerGuid,
    Guid FolloweeGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
