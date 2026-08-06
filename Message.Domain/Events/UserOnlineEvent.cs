
namespace Message.Domain.Events;

public record UserOnlineEvent(
    Guid UserId,
    DateTime OnlineTime) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}