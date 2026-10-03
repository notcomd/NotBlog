
namespace Message.Domain.Events;

/// <summary>群组成员加入事件。</summary>
public record GroupMemberJoinedEvent(
    Guid GroupId,
    Guid UserId,
    GroupMemberRole Role) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}