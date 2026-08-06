
namespace Message.Domain.Events;

public record TopicCreatedEvent(Guid TopicGuid,
    string Name,
    Guid CreatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
