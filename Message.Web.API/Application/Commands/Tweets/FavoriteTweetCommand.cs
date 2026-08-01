namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 收藏推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record FavoriteTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 收藏推文命令处理程序。
/// </summary>
public class FavoriteTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ILogger<FavoriteTweetCommandHandler> logger) : IRequestHandler<FavoriteTweetCommand, bool>
{
    public async Task<bool> Handler(FavoriteTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户收藏推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var exists = await interactionRepository.ExistsAsync(command.TweetGuid, command.UserId, InteractionType.Favorite);
            if (exists)
                throw new InvalidOperationException("已经收藏过了");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            var interaction = TweetInteraction.Create(command.TweetGuid, command.UserId, InteractionType.Favorite);
            await interactionRepository.AddAsync(interaction);
            tweet.AddFavorite();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("收藏成功，推文: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("收藏推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            logger.LogError(ex, "收藏失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
