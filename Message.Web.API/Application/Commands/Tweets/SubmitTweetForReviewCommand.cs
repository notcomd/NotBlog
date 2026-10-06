namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 提交推文审核命令（作者本人将草稿提交审核，Draft → Pending）。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">调用者用户 ID（必须为推文作者）</param>
public record SubmitTweetForReviewCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;
