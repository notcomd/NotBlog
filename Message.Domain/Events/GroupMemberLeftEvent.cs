
namespace Message.Domain.Events;

/// <summary>群组成员退群事件。</summary>
public record GroupMemberLeftEvent(
    Guid GroupId,
    Guid UserId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}