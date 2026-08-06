
namespace Message.Domain.Events;

public record UserUnfollowedEvent(Guid FollowerGuid,
    Guid FolloweeGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
