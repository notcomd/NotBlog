namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取用户时间线推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。R-02/R-07：可见性过滤下沉仓库层（SQL），TotalCount 与列表同条件。</para>
/// <para>时间线语义 = 当前用户自己的已审核全局帖（关注动态走 <c>/follows/feed</c>，见 GetCommunityFeedQuery）。</para>
/// </summary>
public class GetTimelineQueryHandler(
    ITweetRepository tweetRepository) : IRequestHandler<GetTimelineQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetTimelineQuery query, CancellationToken cancellationToken)
    {
        var authorGuids = new[] { query.UserId };

        // 时间线仅含本人帖子：Private/Followers 均命中"作者本人"分支，关注集合传空集即可
        var items = await tweetRepository.GetTimelineAsync(authorGuids, query.UserId, Array.Empty<Guid>(), query.Page, query.PageSize);
        var totalCount = await tweetRepository.GetTimelineCountAsync(authorGuids, query.UserId, Array.Empty<Guid>());

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
