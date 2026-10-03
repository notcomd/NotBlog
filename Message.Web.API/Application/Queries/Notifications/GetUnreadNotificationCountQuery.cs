namespace Message.Web.API.Application.Queries.Notifications;

/// <summary>获取当前用户未读通知数查询。</summary>
public record GetUnreadNotificationCountQuery(Guid UserId) : IRequest<int>;
