
namespace Message.Domain.Events;

public record CircleMemberJoinedEvent(Guid CircleGuid,
    Guid UserGuid,
    Message.Domain.Enums.CircleMemberRole Role,
    Guid? InviterGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
