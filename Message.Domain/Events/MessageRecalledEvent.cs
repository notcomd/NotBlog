using Message.Domain.Enums;
using NotMediator;

namespace Message.Domain.Events;

public record MessageRecalledEvent(
    Guid MessageId,
    Guid RecalledBy,
    RecallReason Reason) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}