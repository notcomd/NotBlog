
namespace Message.Domain.Events;

/// <summary>圈子帖子发布事件。</summary>
public record CirclePostPublishedEvent(Guid TweetGuid,
    Guid AuthorGuid,
    Guid CircleGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
