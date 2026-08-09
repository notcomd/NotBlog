namespace Message.Web.API.Application.Commands.Tweets;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>
/// 投币推文命令处理程序。
/// </summary>
public class CoinTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetInteractionRepository interactionRepository,
    ICircleRepository circleRepository,
    IUserInfoRepository userInfoRepository,
    ILogger<CoinTweetCommandHandler> logger) : IRequestHandler<CoinTweetCommand, bool>
{
    /// <summary>投币行为经验奖励（设计文档 4.3）</summary>
    public const long CoinExperience = 200;

    public async Task<bool> Handler(CoinTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("用户投币推文，用户: {UserGuid}, 推文: {TweetGuid}", command.UserId, command.TweetGuid);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            // 圈子帖：仅圈子成员可互动（作者本人放行）
            await CommunityAccessGuard.EnsureCanInteractWithPostAsync(tweet, command.UserId, circleRepository);

            var interaction = TweetInteraction.Create(command.TweetGuid, command.UserId, InteractionType.Coin);
            await interactionRepository.AddAsync(interaction);
            tweet.AddCoin();
            tweet.RecalculateHotScore();
            await tweetRepository.UpdateAsync(tweet);

            // 设计文档 4.3：投币行为奖励——投币者 +200 经验（同事务；UserInfo 不存在则创建）
            var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
            if (userInfo is null)
            {
                userInfo = UserInfoEntity.Create(command.UserId);
                await userInfoRepository.AddAsync(userInfo);
            }
            var upgraded = userInfo.AddExperience(CoinExperience);
            await userInfoRepository.UpdateAsync(userInfo);

            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("投币成功，推文: {TweetGuid}，投币者 +{Exp} 经验，升级 {Upgraded} 级",
                command.TweetGuid, CoinExperience, upgraded);
            logger.LogInformation("投币推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "投币失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
