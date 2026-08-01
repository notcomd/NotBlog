namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 取消收藏推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record UnfavoriteTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 取消收藏推文命令处理程序。
/// </summary>
public class UnfavoriteTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ILogger<UnfavoriteTweetCommandHandler> logger) : IRequestHandler<UnfavoriteTweetCommand, bool>
{
    public async Task<bool> Handler(UnfavoriteTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户取消收藏推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var interaction = await interactionRepository.GetAsync(command.TweetGuid, command.UserId, InteractionType.Favorite);
            if (interaction == null)
                throw new InvalidOperationException("尚未收藏");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            await interactionRepository.DeleteAsync(command.TweetGuid, command.UserId, InteractionType.Favorite);
            tweet.RemoveFavorite();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("取消收藏成功，推文: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("取消收藏推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            logger.LogError(ex, "取消收藏失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
