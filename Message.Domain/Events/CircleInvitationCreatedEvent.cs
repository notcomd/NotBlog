
namespace Message.Domain.Events;

/// <summary>圈子邀请创建事件。</summary>
public record CircleInvitationCreatedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid InviterGuid,
    Guid? InviteeGuid,
    Message.Domain.Enums.CircleInvitationType Type) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
