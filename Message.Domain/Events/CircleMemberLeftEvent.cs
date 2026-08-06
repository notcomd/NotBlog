
namespace Message.Domain.Events;

public record CircleMemberLeftEvent(Guid CircleGuid,
    Guid UserGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
