using Message.Domain.Enums;
using NotMediator;

namespace Message.Domain.Events;

public record SessionCreatedEvent(
    Guid SessionId,
    HashSet<Guid> Participants,
    SessionType SessionType) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}