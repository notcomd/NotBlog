
namespace Message.Domain.Events;

/// <summary>圈子成员被移出事件。</summary>
public record CircleMemberRemovedEvent(Guid CircleGuid,
    Guid UserGuid,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
