using NotMediator;

namespace Message.Domain.Events;

public record MessageReceivedEvent(
    Guid MessageId,
    Guid ReceiverId,
    DateTime ReceivedTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}