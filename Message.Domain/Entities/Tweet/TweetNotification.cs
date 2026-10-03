
namespace Message.Domain.Entities.Tweet;

/// <summary>
/// 推文通知
/// </summary>
public class TweetNotification : Entity<Guid>
{
    /// <summary>接收用户ID</summary>
    public Guid UserGuid { get; private set; }
    /// <summary>通知类型</summary>
    public NotificationType Type { get; private set; }
    /// <summary>通知标题</summary>
    public string Title { get; private set; } = null!;
    /// <summary>通知内容</summary>
    public string Content { get; private set; } = null!;
    /// <summary>关联对象类型</summary>
    public string? RefType { get; private set; }
    /// <summary>关联对象ID</summary>
    public Guid? RefGuid { get; private set; }
    /// <summary>是否已读</summary>
    public bool IsRead { get; private set; }
    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; private set; }

    private TweetNotification() => Id = Guid.CreateVersion7();

    /// <summary>创建推文通知</summary>
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

    /// <summary>标记为已读</summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }
}
