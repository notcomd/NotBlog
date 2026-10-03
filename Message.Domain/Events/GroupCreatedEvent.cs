
namespace Message.Domain.Events;

/// <summary>群组创建事件。</summary>
public record GroupCreatedEvent(
    Guid GroupId,
    Guid OwnerId,
    string GroupName) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}