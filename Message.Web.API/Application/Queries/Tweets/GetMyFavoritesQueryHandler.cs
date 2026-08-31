namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 我的收藏列表查询处理程序：
/// 以收藏关系（TweetInteraction Type=Favorite）按收藏时间倒序分页，再按 ID 取推文实体并做可见性过滤。
/// </summary>
public class GetMyFavoritesQueryHandler(
    ITweetInteractionRepository interactionRepository,
    ITweetRepository tweetRepository,
    IUserFollowRepository userFollowRepository) : IRequestHandler<GetMyFavoritesQuery, PagedResult<CommunityPostDto>>
{
    public async Task<PagedResult<CommunityPostDto>> Handler(GetMyFavoritesQuery query, CancellationToken cancellationToken)
    {
        var followingIds = (await userFollowRepository.GetFollowingIdsAsync(query.UserId)).ToList();

        var interactions = (await interactionRepository.GetByUserAsync(
                query.UserId, InteractionType.Favorite, query.Page, query.PageSize)).ToList();
        var total = await interactionRepository.GetCountByUserAsync(query.UserId, InteractionType.Favorite);

        // 可见性过滤（作者设为 Private / 未关注作者的 Followers 帖不展示），保持收藏时间顺序
        var tweets = await tweetRepository.GetVisibleByIdsAsync(
            interactions.Select(i => i.TweetGuid), query.UserId, followingIds);

        return new PagedResult<CommunityPostDto>
        {
            Items = tweets.Select(t => t.ToCommunityDto()).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}