
namespace Message.Domain.Events;

public record CirclePostPublishedEvent(Guid TweetGuid,
    Guid AuthorGuid,
    Guid CircleGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
