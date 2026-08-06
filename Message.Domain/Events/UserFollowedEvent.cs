
namespace Message.Domain.Events;

public record UserFollowedEvent(Guid FollowerGuid,
    Guid FolloweeGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
