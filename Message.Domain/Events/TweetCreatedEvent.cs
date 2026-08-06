
namespace Message.Domain.Events;

public record TweetCreatedEvent(
    Guid TweetGuid,
    Guid AuthorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
