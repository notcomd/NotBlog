
namespace Message.Domain.Events;

public record GroupMemberLeftEvent(
    Guid GroupId,
    Guid UserId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}