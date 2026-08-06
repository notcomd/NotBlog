
namespace Message.Domain.Events;

public record TweetInteractionEvent(
    Guid TweetGuid,
    Guid UserGuid,
    InteractionType InteractionType,
    bool IsAdd) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
