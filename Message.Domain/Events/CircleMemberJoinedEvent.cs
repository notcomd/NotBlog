
namespace Message.Domain.Events;

/// <summary>圈子成员加入事件。</summary>
public record CircleMemberJoinedEvent(Guid CircleGuid,
    Guid UserGuid,
    Message.Domain.Enums.CircleMemberRole Role,
    Guid? InviterGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
