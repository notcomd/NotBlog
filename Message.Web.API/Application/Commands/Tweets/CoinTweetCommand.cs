namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 投币推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record CoinTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

