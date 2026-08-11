namespace Message.Web.API.Dto.Response;

/// <summary>站内通知 DTO（TweetNotification 读侧投影）。</summary>
public class NotificationDto
{
    public Guid NotifyGuid { get; init; }
    /// <summary>通知类型（NotificationType 枚举名）</summary>
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    /// <summary>关联目标类型（"Tweet"/"Comment"/"User"/...）</summary>
    public string? RefType { get; init; }
    public Guid? RefGuid { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}
