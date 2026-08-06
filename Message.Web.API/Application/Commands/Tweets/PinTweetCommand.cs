namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 置顶推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">作者用户 ID</param>
public record PinTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

