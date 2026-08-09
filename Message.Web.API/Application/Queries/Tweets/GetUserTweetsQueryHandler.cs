namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取用户推文列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。R-02/R-03/R-07：可见性/状态/圈子过滤下沉仓库层（SQL），TotalCount 与列表同条件。</para>
/// </summary>
public class GetUserTweetsQueryHandler(
    ITweetRepository tweetRepository,
    ICurrentUserService currentUserService,
    IUserFollowRepository followRepository) : IRequestHandler<GetUserTweetsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetUserTweetsQuery query, CancellationToken cancellationToken)
    {
        // S-17/R-03：查看者视角 —— 未认证按 Guid.Empty（仅可见 Public + 圈子帖由仓库层排除）
        var viewerId = currentUserService.IsAuthenticated ? currentUserService.GetUserId() : Guid.Empty;
        var followingIds = viewerId == Guid.Empty
            ? Array.Empty<Guid>()
            : (await followRepository.GetFollowingIdsAsync(viewerId)).ToArray();

        var items = await tweetRepository.GetByAuthorAsync(query.UserGuid, viewerId, followingIds, query.Page, query.PageSize);
        var totalCount = await tweetRepository.GetCountByAuthorAsync(query.UserGuid, viewerId, followingIds);

        return new PagedResult<Tweet>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
