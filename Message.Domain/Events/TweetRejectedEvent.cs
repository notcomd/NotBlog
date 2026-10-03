
namespace Message.Domain.Events;

/// <summary>推文审核驳回事件。</summary>
public record TweetRejectedEvent(
    Guid TweetGuid,
    Guid AuthorGuid,
    Guid AuditorGuid,
    string Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
