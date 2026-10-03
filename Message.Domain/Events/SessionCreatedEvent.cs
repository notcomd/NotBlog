
namespace Message.Domain.Events;

/// <summary>会话创建事件。</summary>
public record SessionCreatedEvent(
    Guid SessionId,
    HashSet<Guid> Participants,
    SessionType SessionType) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}