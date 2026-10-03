
namespace Message.Domain.Events;

/// <summary>推文创建事件。</summary>
public record TweetCreatedEvent(
    Guid TweetGuid,
    Guid AuthorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
