
namespace Message.Domain.Events;

/// <summary>
/// 评论添加事件
/// </summary>
public record CommentAddedEvent(
    Guid CommentGuid,
    Guid TweetGuid,
    Guid UserGuid,
    Guid? ParentGuid) : INotifications
{
    /// <summary>
    /// 生事件时间
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
