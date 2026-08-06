
namespace Message.Domain.Events;

public record UserOfflineEvent(
    Guid UserId,
    DateTime OfflineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}