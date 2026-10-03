
namespace Message.Domain.Events;

/// <summary>圈子解散事件。</summary>
public record CircleDissolvedEvent(Guid CircleGuid,
    Guid OwnerGuid) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
