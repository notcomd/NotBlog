namespace Message.Web.API.Application.Queries.Notifications;

/// <summary>获取当前用户通知列表查询处理程序。</summary>
public class GetMyNotificationsQueryHandler(
    ITweetNotificationRepository notificationRepository) : IRequestHandler<GetMyNotificationsQuery, PagedResult<TweetNotification>>
{
    public async Task<PagedResult<TweetNotification>> Handler(GetMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        var items = await notificationRepository.GetByUserAsync(query.UserId, query.UnreadOnly, query.Page, query.PageSize);
        var totalCount = await notificationRepository.CountByUserAsync(query.UserId, query.UnreadOnly);

        return new PagedResult<TweetNotification>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}