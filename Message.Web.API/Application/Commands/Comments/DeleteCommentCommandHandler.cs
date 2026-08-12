namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 删除评论命令处理程序。
/// <para>权限：评论作者本人 / 全局管理员 / 评论所属频道（圈子）帖的圈主或圈管理员（频道内容管理）。</para>
/// </summary>
public class DeleteCommentCommandHandler(
    ICommentRepository commentRepository,
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
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
            {
                // 频道内容管理：评论落在圈子帖上时，圈主/圈管理员可删除
                var tweet = await tweetRepository.GetByIdAsync(comment.TweetGuid);
                if (tweet?.CircleGuid is null)
                    throw new UnauthorizedAccessException("无权删除此评论");
                var member = await circleRepository.GetMemberAsync(tweet.CircleGuid.Value, command.UserId);
                if (member is null || member.Status != CircleMemberStatus.Active
                    || (member.Role != CircleMemberRole.Owner && member.Role != CircleMemberRole.Admin))
                    throw new UnauthorizedAccessException("无权删除此评论");
            }

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
