
namespace Message.Domain.Events;

public record GroupCreatedEvent(
    Guid GroupId,
    Guid OwnerId,
    string GroupName) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}