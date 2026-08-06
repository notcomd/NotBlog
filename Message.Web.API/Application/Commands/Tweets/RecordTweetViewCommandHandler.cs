namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 记录推文查看命令处理程序。
/// </summary>
public class RecordTweetViewCommandHandler(
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ILogger<RecordTweetViewCommandHandler> logger) : IRequestHandler<RecordTweetViewCommand, bool>
{
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
