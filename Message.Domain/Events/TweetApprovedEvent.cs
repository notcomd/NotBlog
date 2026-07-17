using NotMediator;

namespace Message.Domain.Events;

public record TweetApprovedEvent(
    Guid TweetGuid,
    Guid AuthorGuid,
    Guid AuditorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
