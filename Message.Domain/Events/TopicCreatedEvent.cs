
namespace Message.Domain.Events;

/// <summary>话题创建事件。</summary>
public record TopicCreatedEvent(Guid TopicGuid,
    string Name,
    Guid CreatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
