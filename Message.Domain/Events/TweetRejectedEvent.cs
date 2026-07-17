using NotMediator;

namespace Message.Domain.Events;

public record TweetRejectedEvent(
    Guid TweetGuid,
    Guid AuthorGuid,
    Guid AuditorGuid,
    string Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
