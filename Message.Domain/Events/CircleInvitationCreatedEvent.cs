
namespace Message.Domain.Events;

public record CircleInvitationCreatedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid InviterGuid,
    Guid? InviteeGuid,
    Message.Domain.Enums.CircleInvitationType Type) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
