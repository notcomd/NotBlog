namespace Message.Web.API.Dto.Response;

/// <summary>通知实体 → DTO 映射。</summary>
public static class NotificationMappingExtensions
{
    public static NotificationDto ToDto(this TweetNotification notification) => new()
    {
        NotifyGuid = notification.Id,
        Type = notification.Type.ToString(),
        Title = notification.Title,
        Content = notification.Content,
        RefType = notification.RefType,
        RefGuid = notification.RefGuid,
        IsRead = notification.IsRead,
        CreateTime = notification.CreateTime
    };
}
