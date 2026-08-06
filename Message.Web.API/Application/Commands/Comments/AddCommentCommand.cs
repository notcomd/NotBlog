namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 发布评论命令。
/// <para>CQRS 命令侧：仅返回新评论的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">评论者用户 ID</param>
/// <param name="Content">评论内容</param>
/// <param name="ParentGuid">父评论 ID（回复时可选）</param>
/// <param name="ReplyToGuid">被回复评论 ID（回复时可选）</param>
public record AddCommentCommand(
    Guid TweetGuid,
    Guid UserId,
    string Content,
    Guid? ParentGuid,
    Guid? ReplyToGuid) : IRequest<Guid>;

