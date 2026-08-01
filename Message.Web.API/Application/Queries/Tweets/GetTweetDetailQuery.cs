namespace Message.Web.API.Application.Queries.Tweets;

/// <summary>
/// 推文详情结果。
/// </summary>
/// <param name="Tweet">推文实体（不存在时为 null）</param>
/// <param name="IsLiked">当前用户是否已点赞</param>
/// <param name="IsFavorited">当前用户是否已收藏</param>
/// <param name="IsCoined">当前用户是否已投币</param>
public record TweetDetailResult(Tweet? Tweet, bool IsLiked, bool IsFavorited, bool IsCoined);

/// <summary>
/// 获取推文详情查询。
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="CurrentUserId">当前用户 ID（用于获取交互状态）</param>
public record GetTweetDetailQuery(Guid TweetGuid, Guid CurrentUserId) : IRequest<TweetDetailResult>;

/// <summary>
/// 获取推文详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetTweetDetailQueryHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository) : IRequestHandler<GetTweetDetailQuery, TweetDetailResult>
{
    public async Task<TweetDetailResult> Handler(GetTweetDetailQuery query, CancellationToken cancellationToken)
    {
        var tweet = await tweetRepository.GetByIdAsync(query.TweetGuid);

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
