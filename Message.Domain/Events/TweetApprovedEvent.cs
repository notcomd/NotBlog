
namespace Message.Domain.Events;

/// <summary>推文审核通过事件。</summary>
public record TweetApprovedEvent(
    Guid TweetGuid,
    Guid AuthorGuid,
    Guid AuditorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
