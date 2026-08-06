
namespace Message.Domain.Events;

public record CircleMemberRoleChangedEvent(Guid CircleGuid,
    Guid UserGuid,
    Message.Domain.Enums.CircleMemberRole OldRole,
    Message.Domain.Enums.CircleMemberRole NewRole,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
