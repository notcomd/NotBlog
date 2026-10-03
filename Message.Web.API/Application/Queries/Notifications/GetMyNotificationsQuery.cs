namespace Message.Web.API.Application.Queries.Notifications;

/// <summary>获取当前用户通知列表查询（分页，可按未读过滤）。</summary>
public record GetMyNotificationsQuery(Guid UserId, int Page, int PageSize, bool UnreadOnly = false) : IRequest<PagedResult<TweetNotification>>;
