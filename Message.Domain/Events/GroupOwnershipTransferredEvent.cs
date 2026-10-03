
namespace Message.Domain.Events;

/// <summary>群组所有权转移事件。</summary>
public record GroupOwnershipTransferredEvent(
    Guid GroupId,
    Guid OldOwnerId,
    Guid NewOwnerId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
