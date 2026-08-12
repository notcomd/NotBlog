namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 删除评论命令（作者本人 / 全局管理员 / 频道圈主与圈管理员）。
/// </summary>
/// <param name="CommentGuid">评论 ID</param>
/// <param name="UserId">删除者用户 ID</param>
public record DeleteCommentCommand(Guid CommentGuid, Guid UserId) : IRequest<bool>;

