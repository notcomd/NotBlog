
namespace Message.Domain.Events;

public record CircleInvitationAcceptedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid UserGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
