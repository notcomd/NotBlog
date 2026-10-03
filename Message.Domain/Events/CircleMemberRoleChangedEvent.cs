
namespace Message.Domain.Events;

/// <summary>圈子成员角色变更事件。</summary>
public record CircleMemberRoleChangedEvent(Guid CircleGuid,
    Guid UserGuid,
    Message.Domain.Enums.CircleMemberRole OldRole,
    Message.Domain.Enums.CircleMemberRole NewRole,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
