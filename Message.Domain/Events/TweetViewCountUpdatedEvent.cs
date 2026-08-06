
namespace Message.Domain.Events;

public record TweetViewCountUpdatedEvent(
    Guid TweetGuid,
    long ViewCount) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
