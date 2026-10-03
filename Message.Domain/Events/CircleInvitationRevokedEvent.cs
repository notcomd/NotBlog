
namespace Message.Domain.Events;

/// <summary>圈子邀请被撤销事件。</summary>
public record CircleInvitationRevokedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
