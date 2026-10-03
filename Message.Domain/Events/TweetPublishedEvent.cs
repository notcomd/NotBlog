
namespace Message.Domain.Events;

/// <summary>推文发布事件。</summary>
public record TweetPublishedEvent(
    Guid TweetGuid,
    Guid AuthorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
