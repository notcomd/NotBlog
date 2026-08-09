namespace Message.Web.API.Application.Queries.Community;

/// <summary>关注 Feed 查询处理程序（R-02：SQL 层排除 Private，TotalCount 同条件）。</summary>
public class GetCommunityFeedQueryHandler(
    IUserFollowRepository followRepository,
    ITweetRepository tweetRepository) : IRequestHandler<GetCommunityFeedQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetCommunityFeedQuery query, CancellationToken cancellationToken)
    {
        var followingIds = (await followRepository.GetFollowingIdsAsync(query.UserId)).ToArray();
        var authorGuids = followingIds.Append(query.UserId).Distinct().ToArray();

        // Feed 场景查看者即 query.UserId：Followers 推文天然可见（作者必在关注列表），Private 由仓库层排除
        var items = await tweetRepository.GetCommunityFeedAsync(authorGuids, query.UserId, followingIds, query.Page, query.PageSize);
        var total = await tweetRepository.GetCommunityFeedCountAsync(authorGuids, query.UserId, followingIds);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
