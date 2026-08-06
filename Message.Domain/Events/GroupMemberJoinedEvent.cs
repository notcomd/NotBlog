
namespace Message.Domain.Events;

public record GroupMemberJoinedEvent(
    Guid GroupId,
    Guid UserId,
    GroupMemberRole Role) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}