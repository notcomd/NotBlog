
namespace Message.Domain.Events;

public record CircleMemberRemovedEvent(Guid CircleGuid,
    Guid UserGuid,
    Guid OperatorGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
