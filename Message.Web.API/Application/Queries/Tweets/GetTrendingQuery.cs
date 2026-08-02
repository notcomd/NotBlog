namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 获取趋势推文列表查询。
/// </summary>
/// <param name="Page">页码（从1开始）</param>
/// <param name="PageSize">每页条数</param>
public record GetTrendingQuery(int Page, int PageSize) : IRequest<PagedResult<Tweet>>;

/// <summary>
/// 获取趋势推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTrendingQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTrendingQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetTrendingQuery query, CancellationToken cancellationToken)
    {
        // S-17：可见性过滤 —— 趋势/热榜为公开流，仅展示 Public 推文（Private/Followers 默认排除）
        var items = (await tweetRepository.GetTrendingAsync(query.Page, query.PageSize))
            .Where(t => t.Visibility == Visibility.Public);
        var totalCount = await tweetRepository.GetTrendingCountAsync();

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}