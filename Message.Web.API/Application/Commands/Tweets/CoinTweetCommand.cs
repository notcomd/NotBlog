namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 投币推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record CoinTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 投币推文命令处理程序。
/// </summary>
public class CoinTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ILogger<CoinTweetCommandHandler> logger) : IRequestHandler<CoinTweetCommand, bool>
{
    public async Task<bool> Handler(CoinTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户投币推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(command.TweetGuid, command.UserId, InteractionType.Coin);
            await interactionRepository.AddAsync(interaction);
            tweet.AddCoin();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("投币成功，推文: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("投币推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "投币失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
