
namespace Message.Domain.Events;

/// <summary>用户上线事件。</summary>
public record UserOnlineEvent(
    Guid UserId,
    DateTime OnlineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}