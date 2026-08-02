using Message.Infrastructure.Services;

namespace Message.Web.API.Application.Queries.Sessions;

/// <summary>
/// 获取用户所有会话未读消息总数查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetTotalUnreadCountQuery(Guid UserId) : IRequest<int>;

/// <summary>
/// 获取用户所有会话未读消息总数查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>Q-05：读时先查未读计数缓存，命中直接返回；miss 回源 DB（ChatSession 未读汇总）并回填缓存（TTL 1h）。
/// 缓存采用"写时失效"策略（发送/已读路径删除缓存键），保证与 DB 数据一致。</para>
/// </summary>
public class GetTotalUnreadCountQueryHandler(
    IChatSessionRepository sessionRepository,
    UnreadCountCacheService unreadCountCache) : IRequestHandler<GetTotalUnreadCountQuery, int>
{
    public async Task<int> Handler(GetTotalUnreadCountQuery query, CancellationToken cancellationToken)
    {
        var cached = await unreadCountCache.TryGetTotalUnreadCountAsync(query.UserId, cancellationToken);
        if (cached.HasValue)
            return cached.Value;

        var total = await sessionRepository.GetTotalUnreadCountAsync(query.UserId);
        await unreadCountCache.SetTotalUnreadCountAsync(query.UserId, total, cancellationToken);
        return total;
    }
}
