
namespace Message.Domain.Events;

public record MessageForwardedEvent(
    Guid OriginalMessageId,
    Guid ForwardedMessageId,
    Guid ForwardedBy,
    Guid TargetSessionId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}