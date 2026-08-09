namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取推文详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。R-03：Followers 可见性接入关注关系；R-06：移除自动浏览计数（统一走 POST /view）。</para>
/// </summary>
public class GetTweetDetailQueryHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ICircleRepository circleRepository,
    IUserFollowRepository followRepository) : IRequestHandler<GetTweetDetailQuery, TweetDetailResult>
{
    public async Task<TweetDetailResult> Handler(GetTweetDetailQuery query, CancellationToken cancellationToken)
    {
        var tweet = await tweetRepository.GetByIdAsync(query.TweetGuid);

        // R-03：一次查询查看者的关注集合（Followers 可见性判定，避免逐条 N+1）
        var followingIds = query.CurrentUserId == Guid.Empty
            ? new HashSet<Guid>()
            : (await followRepository.GetFollowingIdsAsync(query.CurrentUserId)).ToHashSet();

        // S-17/R-03：可见性过滤 —— Private 仅作者可见；Followers 仅关注者可见；不可见视为不存在
        if (tweet != null && !TweetVisibilityPolicy.IsVisibleTo(tweet, query.CurrentUserId, followingIds))
            return new TweetDetailResult(null, false, false, false);

        // 圈子帖：仅圈子成员可见（作者本人放行），非成员视为不存在
        if (tweet != null && tweet.CircleGuid is not null
            && !await CommunityAccessGuard.IsPostVisibleToAsync(tweet, query.CurrentUserId, circleRepository))
            return new TweetDetailResult(null, false, false, false);

        // R-06：详情查询不再自动 +1 浏览量（避免 GET 刷量 + 每次读库写库），计数唯一入口为 POST /{tweetGuid}/view

        if (tweet == null)
            return new TweetDetailResult(null, false, false, false);

        var isLiked = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Like);
        var isFavorited = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Favorite);
        var isCoined = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Coin);

        return new TweetDetailResult(tweet, isLiked, isFavorited, isCoined);
    }
}
