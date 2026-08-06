
namespace Message.Domain.Events;

public record CircleDissolvedEvent(Guid CircleGuid,
    Guid OwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
