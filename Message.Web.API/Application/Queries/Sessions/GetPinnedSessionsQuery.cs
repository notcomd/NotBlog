namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取用户置顶会话列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetPinnedSessionsQuery(Guid UserId) : IRequest<IEnumerable<ChatSession>>;

/// <summary>
/// 获取用户置顶会话列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetPinnedSessionsQueryHandler(
    IChatSessionRepository sessionRepository) : IRequestHandler<GetPinnedSessionsQuery, IEnumerable<ChatSession>>
{
    public async Task<IEnumerable<ChatSession>> Handler(GetPinnedSessionsQuery query, CancellationToken cancellationToken)
        => await sessionRepository.GetPinnedSessionsAsync(query.UserId);
}
