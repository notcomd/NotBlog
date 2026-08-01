namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 记录推文查看命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record RecordTweetViewCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 记录推文查看命令处理程序。
/// </summary>
public class RecordTweetViewCommandHandler(
    ITweetRepository tweetRepository,
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
