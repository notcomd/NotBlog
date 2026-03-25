using NotMediator;

namespace Message.Domain.Events;

public record GroupDissolvedEvent(
    Guid GroupId,
    Guid DissolvedBy,
    DateTime DissolvedTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
