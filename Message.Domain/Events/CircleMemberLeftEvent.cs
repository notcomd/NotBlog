
namespace Message.Domain.Events;

/// <summary>圈子成员退出事件。</summary>
public record CircleMemberLeftEvent(Guid CircleGuid,
    Guid UserGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
