using NotMediator;

namespace Message.Domain.Events;

public record FriendshipCreatedEvent(
    Guid UserId,
    Guid FriendId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}