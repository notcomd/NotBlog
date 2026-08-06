namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 删除评论命令（仅限作者本人）。
/// </summary>
/// <param name="CommentGuid">评论 ID</param>
/// <param name="UserId">删除者用户 ID</param>
public record DeleteCommentCommand(Guid CommentGuid, Guid UserId) : IRequest<bool>;

