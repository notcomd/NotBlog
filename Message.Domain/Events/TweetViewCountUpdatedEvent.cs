
namespace Message.Domain.Events;

/// <summary>推文浏览量更新事件。</summary>
public record TweetViewCountUpdatedEvent(
    Guid TweetGuid,
    long ViewCount) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
