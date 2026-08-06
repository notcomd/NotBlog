namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取推文详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTweetDetailQueryHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ICircleRepository circleRepository) : IRequestHandler<GetTweetDetailQuery, TweetDetailResult>
{
    public async Task<TweetDetailResult> Handler(GetTweetDetailQuery query, CancellationToken cancellationToken)
    {
        var tweet = await tweetRepository.GetByIdAsync(query.TweetGuid);

        // S-17：可见性过滤 —— Private 仅作者可见；Followers 无关注关系实现退化为仅作者可见；非作者视为不存在
        if (tweet != null && !TweetVisibilityPolicy.IsVisibleTo(tweet, query.CurrentUserId))
            return new TweetDetailResult(null, false, false, false);

        // 圈子帖：仅圈子成员可见（作者本人放行），非成员视为不存在
        if (tweet != null && tweet.CircleGuid is not null
            && !await CommunityAccessGuard.IsPostVisibleToAsync(tweet, query.CurrentUserId, circleRepository))
            return new TweetDetailResult(null, false, false, false);

        if (tweet != null && tweet.TweetStatus == TweetStatus.Approved)
        {
            tweet.IncrementViewCount();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        if (tweet == null)
            return new TweetDetailResult(null, false, false, false);

        var isLiked = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Like);
        var isFavorited = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Favorite);
        var isCoined = await interactionRepository.ExistsAsync(query.TweetGuid, query.CurrentUserId, InteractionType.Coin);

        return new TweetDetailResult(tweet, isLiked, isFavorited, isCoined);
    }
}
