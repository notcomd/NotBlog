using Message.Domain.Enums;

namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文通知
/// </summary>
public class TweetNotification
{
    public Guid NotifyGuid { get; init; }
    public Guid UserGuid { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public string? RefType { get; private set; }
    public Guid? RefGuid { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreateTime { get; private set; }

    private TweetNotification() => NotifyGuid = Guid.CreateVersion7();

    public static TweetNotification Create(
        Guid userGuid, NotificationType type, string title, string content,
        string? refType = null, Guid? refGuid = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("通知标题不能为空", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("通知内容不能为空", nameof(content));

        return new TweetNotification
        {
            UserGuid = userGuid,
            Type = type,
            Title = title,
            Content = content,
            RefType = refType,
            RefGuid = refGuid,
            IsRead = false,
            CreateTime = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
