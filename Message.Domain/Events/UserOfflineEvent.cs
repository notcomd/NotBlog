
namespace Message.Domain.Events;

/// <summary>用户下线事件。</summary>
public record UserOfflineEvent(
    Guid UserId,
    DateTime OfflineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}