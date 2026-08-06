namespace Message.Web.API.Application.Queries.Tweets;
/// <summary>
/// 获取推文详情查询。
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="CurrentUserId">当前用户 ID（用于获取交互状态）</param>
public record GetTweetDetailQuery(Guid TweetGuid, Guid CurrentUserId) : IRequest<TweetDetailResult>;

