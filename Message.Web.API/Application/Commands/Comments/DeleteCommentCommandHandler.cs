namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 删除评论命令处理程序。
/// </summary>
public class DeleteCommentCommandHandler(
    ICommentRepository commentRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteCommentCommandHandler> logger) : IRequestHandler<DeleteCommentCommand, bool>
{
    public async Task<bool> Handler(DeleteCommentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始删除评论，ID: {CommentGuid}", command.CommentGuid);

            var comment = await commentRepository.GetByIdAsync(command.CommentGuid);
            if (comment == null)
                throw new KeyNotFoundException("评论不存在");

            if (comment.UserGuid != command.UserId && !currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("无权删除此评论");

            await commentRepository.DeleteAsync(command.CommentGuid);
            await commentRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("评论删除成功，ID: {CommentGuid}", command.CommentGuid);
            logger.LogInformation("评论 {CommentGuid} 已删除，用户={UserId}", command.CommentGuid, command.UserId);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "删除评论失败，ID: {CommentGuid}", command.CommentGuid);
            throw;
        }
    }
}
