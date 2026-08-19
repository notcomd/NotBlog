namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 记录推文查看命令处理程序。
/// <para>R-06：Redis Set 按用户去重（24h 窗口），同一用户重复浏览只计一次；GET 详情已移除自动计数，本端点是浏览计数唯一入口。</para>
/// </summary>
public class RecordTweetViewCommandHandler(
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ILogger<RecordTweetViewCommandHandler> logger,
     MessageCacheService redisCache) : IRequestHandler<RecordTweetViewCommand, bool>
{
    /// <summary>同用户浏览去重窗口（24 小时）</summary>
    private static readonly TimeSpan ViewDedupWindow = TimeSpan.FromHours(24);

    public async Task<bool> Handler(RecordTweetViewCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
            {
                logger.LogWarning("记录查看时推文不存在，ID: {TweetGuid}", command.TweetGuid);
                return true;
            }

            // 圈子帖：仅圈子成员查看才计数（作者本人放行）
            await CommunityAccessGuard.EnsureCanInteractWithPostAsync(tweet, command.UserId, circleRepository);

            // R-06：24h 内同用户去重（SADD 原子；新 Set 自动设置 TTL 防膨胀）
            var dedupKey = $"tweet:viewed:{command.TweetGuid:N}";
            var isFirstView = await redisCache.SetAddAsync(
                dedupKey, command.UserId.ToString("N"), ViewDedupWindow, cancellationToken);
            if (!isFirstView)
            {
                logger.LogDebug("重复浏览已去重：TweetGuid={TweetGuid}, UserId={UserId}", command.TweetGuid, command.UserId);
                return true;
            }

            tweet.IncrementViewCount();

            if (tweet.ViewCount % 10 == 0)
            {
                tweet.RecalculateHotScore();
            }

            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("记录推文查看成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "记录查看失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
