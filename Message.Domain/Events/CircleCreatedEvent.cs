
namespace Message.Domain.Events;

public record CircleCreatedEvent(Guid CircleGuid,
    Guid OwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
