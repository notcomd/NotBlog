
namespace Message.Domain.Events;

public record CircleOwnerTransferredEvent(Guid CircleGuid,
    Guid OldOwnerGuid,
    Guid NewOwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
