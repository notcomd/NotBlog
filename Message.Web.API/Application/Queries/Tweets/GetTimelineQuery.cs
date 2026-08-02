namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取用户时间线推文列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTimelineQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

/// <summary>
/// 获取用户时间线推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTimelineQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTimelineQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetTimelineQuery query, CancellationToken cancellationToken)
    {
        var authorGuids = new[] { query.UserId };
        // S-17：可见性过滤 —— 时间线内仅保留对当前用户可见的推文（非作者的 Private/Followers 被排除）
        var items = (await tweetRepository.GetTimelineAsync(authorGuids, query.Page, query.PageSize))
            .Where(t => TweetVisibilityPolicy.IsVisibleTo(t, query.UserId));
        var totalCount = await tweetRepository.GetTimelineCountAsync(authorGuids);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}