
namespace Message.Domain.Events;

/// <summary>圈子圈主转移事件。</summary>
public record CircleOwnerTransferredEvent(Guid CircleGuid,
    Guid OldOwnerGuid,
    Guid NewOwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
