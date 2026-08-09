namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取趋势推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。R-07：Public-only 过滤下沉仓库层（SQL），TotalCount 与列表同条件。</para>
/// </summary>
public class GetTrendingQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTrendingQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetTrendingQuery query, CancellationToken cancellationToken)
    {
        // S-17/R-07：趋势/热榜为公开流，仅展示 Public 推文（仓库层过滤，Private/Followers 排除）
        var items = await tweetRepository.GetTrendingAsync(query.Page, query.PageSize);
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
