
namespace Message.Domain.Events;

/// <summary>群组解散事件。</summary>
public record GroupDissolvedEvent(
    Guid GroupId,
    Guid DissolvedBy,
    DateTime DissolvedTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
