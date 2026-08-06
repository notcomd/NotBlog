namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 取消点赞推文命令处理程序。
/// </summary>
public class UnlikeTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ICircleRepository circleRepository,
    ILogger<UnlikeTweetCommandHandler> logger) : IRequestHandler<UnlikeTweetCommand, bool>
{
    public async Task<bool> Handler(UnlikeTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户取消点赞推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var interaction = await interactionRepository.GetAsync(command.TweetGuid, command.UserId, InteractionType.Like);
            if (interaction == null)
                throw new InvalidOperationException("尚未点赞");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            // 圈子帖：仅圈子成员可互动（作者本人放行）
            await CommunityAccessGuard.EnsureCanInteractWithPostAsync(tweet, command.UserId, circleRepository);

            await interactionRepository.DeleteAsync(command.TweetGuid, command.UserId, InteractionType.Like);
            tweet.RemoveLike();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("取消点赞成功，推文: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("取消点赞推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "取消点赞失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
