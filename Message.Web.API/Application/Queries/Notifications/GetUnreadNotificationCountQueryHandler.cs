namespace Message.Web.API.Application.Queries.Notifications;

/// <summary>获取当前用户未读通知数查询处理程序。</summary>
public class GetUnreadNotificationCountQueryHandler(
    ITweetNotificationRepository notificationRepository) : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<int> Handler(GetUnreadNotificationCountQuery query, CancellationToken cancellationToken)
    {
        return await notificationRepository.GetUnreadCountAsync(query.UserId);
    }
}