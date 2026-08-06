namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 记录推文查看命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">用户 ID</param>
public record RecordTweetViewCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

