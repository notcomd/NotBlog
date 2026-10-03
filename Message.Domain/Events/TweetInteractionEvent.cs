
namespace Message.Domain.Events;

/// <summary>推文互动事件（点赞/收藏等，IsAdd 区分新增与取消）。</summary>
public record TweetInteractionEvent(
    Guid TweetGuid,
    Guid UserGuid,
    InteractionType InteractionType,
    bool IsAdd) : INotifications
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
