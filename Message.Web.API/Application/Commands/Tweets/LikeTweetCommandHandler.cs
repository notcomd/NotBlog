namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 点赞推文命令处理程序。
/// </summary>
public class LikeTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ICircleRepository circleRepository,
    ILogger<LikeTweetCommandHandler> logger) : IRequestHandler<LikeTweetCommand, bool>
{
    public async Task<bool> Handler(LikeTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户点赞推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var exists = await interactionRepository.ExistsAsync(command.TweetGuid, command.UserId, InteractionType.Like);
            if (exists)
                throw new InvalidOperationException("已经点过赞了");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            // 圈子帖：仅圈子成员可互动（作者本人放行）
            await CommunityAccessGuard.EnsureCanInteractWithPostAsync(tweet, command.UserId, circleRepository);

            var interaction = TweetInteraction.Create(command.TweetGuid, command.UserId, InteractionType.Like);
            await interactionRepository.AddAsync(interaction);
            tweet.AddLike();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("点赞成功，推文: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("点赞推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "点赞失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
