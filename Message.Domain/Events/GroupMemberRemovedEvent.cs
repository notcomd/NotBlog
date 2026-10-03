
namespace Message.Domain.Events;

/// <summary>群组成员被移出事件。</summary>
public record GroupMemberRemovedEvent(Guid GroupId, Guid UserId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}