using Message.Domain.Enums;
using NotMediator;

namespace Message.Domain.Events;

public record MessageSentEvent(
    Guid MessageId,
    Guid SenderId,
    Guid? ReceiverId,
    Guid SessionId,
    MessageType MessageType) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}