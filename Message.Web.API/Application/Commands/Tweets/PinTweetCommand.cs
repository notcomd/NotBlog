namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 置顶推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">作者用户 ID</param>
public record PinTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 置顶推文命令处理程序。
/// </summary>
public class PinTweetCommandHandler(
    ITweetRepository tweetRepository,
    ILogger<PinTweetCommandHandler> logger) : IRequestHandler<PinTweetCommand, bool>
{
    public async Task<bool> Handler(PinTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始置顶推文，ID: {TweetGuid}", command.TweetGuid);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != command.UserId)
                throw new UnauthorizedAccessException("无权置顶此推文");

            tweet.Pin();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文置顶成功，ID: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("置顶推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "置顶推文失败，ID: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
