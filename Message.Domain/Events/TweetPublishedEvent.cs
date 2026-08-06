
namespace Message.Domain.Events;

public record TweetPublishedEvent(
    Guid TweetGuid,
    Guid AuthorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
