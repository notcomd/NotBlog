
namespace Message.Domain.Events;

public record GroupMemberRemovedEvent(Guid GroupId, Guid UserId) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}