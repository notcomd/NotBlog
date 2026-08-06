
namespace Message.Domain.Events;

public record GroupOwnershipTransferredEvent(
    Guid GroupId,
    Guid OldOwnerId,
    Guid NewOwnerId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
