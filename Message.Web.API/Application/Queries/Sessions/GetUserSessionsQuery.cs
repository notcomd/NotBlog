namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取用户会话列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetUserSessionsQuery(Guid UserId) : IRequest<IEnumerable<ChatSession>>;

/// <summary>
/// 获取用户会话列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUserSessionsQueryHandler(
    IChatSessionRepository sessionRepository) : IRequestHandler<GetUserSessionsQuery, IEnumerable<ChatSession>>
{
    public async Task<IEnumerable<ChatSession>> Handler(GetUserSessionsQuery query, CancellationToken cancellationToken)
        => await sessionRepository.GetByUserIdAsync(query.UserId);
}
