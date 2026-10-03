
namespace Message.Domain.Events;

/// <summary>圈子邀请被接受事件。</summary>
public record CircleInvitationAcceptedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid UserGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
