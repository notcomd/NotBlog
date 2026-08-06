namespace Message.Web.API.Application.Queries.Community;

/// <summary>关注 Feed 查询处理程序。</summary>
public class GetCommunityFeedQueryHandler(
    IUserFollowRepository followRepository,
    ITweetRepository tweetRepository) : IRequestHandler<GetCommunityFeedQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetCommunityFeedQuery query, CancellationToken cancellationToken)
    {
        var followingIds = await followRepository.GetFollowingIdsAsync(query.UserId);
        var authorGuids = followingIds.Append(query.UserId).Distinct().ToArray();

        var items = await tweetRepository.GetCommunityFeedAsync(authorGuids, query.Page, query.PageSize);
        var total = await tweetRepository.GetCommunityFeedCountAsync(authorGuids);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
