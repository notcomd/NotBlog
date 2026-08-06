
namespace Message.Domain.Events;

public record CircleInvitationRevokedEvent(Guid InviteGuid,
    Guid CircleGuid,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
