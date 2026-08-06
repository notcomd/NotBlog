
namespace Message.Domain.Events;

public record FriendshipAcceptedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}