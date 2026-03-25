using NotMediator;

namespace Message.Domain.Events;

public record MessageReadEvent(
    Guid MessageId,
    Guid ReaderId,
    DateTime ReadTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}