
namespace Message.Domain.Events;

public record FriendshipRejectedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
