
namespace Message.Domain.Events;

/// <summary>圈子创建事件。</summary>
public record CircleCreatedEvent(Guid CircleGuid,
    Guid OwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
