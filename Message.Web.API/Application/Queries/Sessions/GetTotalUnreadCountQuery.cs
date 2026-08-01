namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取用户所有会话未读消息总数查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetTotalUnreadCountQuery(Guid UserId) : IRequest<int>;

/// <summary>
/// 获取用户所有会话未读消息总数查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTotalUnreadCountQueryHandler(
    IChatSessionRepository sessionRepository) : IRequestHandler<GetTotalUnreadCountQuery, int>
{
    public async Task<int> Handler(GetTotalUnreadCountQuery query, CancellationToken cancellationToken)
        => await sessionRepository.GetTotalUnreadCountAsync(query.UserId);
}
