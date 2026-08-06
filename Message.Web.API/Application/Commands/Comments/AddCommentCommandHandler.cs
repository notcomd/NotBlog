namespace Message.Web.API.Application.Commands.Comments;
/// <summary>
/// 发布评论命令处理程序。
/// </summary>
public class AddCommentCommandHandler(
    ICommentRepository commentRepository,
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ILogger<AddCommentCommandHandler> logger) : IRequestHandler<AddCommentCommand, Guid>
{
    public async Task<Guid> Handler(AddCommentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始添加评论，推文: {TweetGuid}, 用户: {UserGuid}", command.TweetGuid, command.UserId);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.TweetStatus != TweetStatus.Approved)
                throw new InvalidOperationException("只有已审核通过的推文才能评论");

            // 圈子帖：仅圈子成员可评论（作者本人放行）
            await CommunityAccessGuard.EnsureCanInteractWithPostAsync(tweet, command.UserId, circleRepository);

            // 检查回复嵌套层级（最多2层）
            if (command.ParentGuid.HasValue)
            {
                var parentComment = await commentRepository.GetByIdAsync(command.ParentGuid.Value);
                if (parentComment == null)
                    throw new KeyNotFoundException("父评论不存在");

                if (parentComment.ParentGuid.HasValue)
                    throw new InvalidOperationException("评论嵌套层级不能超过2层");
            }

            // S-17：内容净化 + 空值校验 + 长度校验（上限 500 字符，实体层 Comment.Create 亦有兜底校验）
            var safeContent = SafeContentSanitizer.Sanitize(command.Content);
            if (string.IsNullOrWhiteSpace(safeContent))
                throw new ArgumentException("评论内容不能为空");
            if (safeContent.Length > 500)
                throw new ArgumentException("评论内容不能超过500个字符");

            var comment = Comment.Create(command.TweetGuid, command.UserId, safeContent,
                command.ParentGuid, command.ReplyToGuid);
            await commentRepository.AddAsync(comment);

            tweet.AddComment();
            await tweetRepository.UpdateAsync(tweet);

            if (command.ParentGuid.HasValue)
            {
                var parentComment = await commentRepository.GetByIdAsync(command.ParentGuid.Value);
                if (parentComment != null)
                {
                    parentComment.IncrementReplyCount();
                    await commentRepository.UpdateAsync(parentComment);
                }
            }

            await commentRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("评论添加成功，ID: {CommentGuid}", comment.CommentGuid);
            logger.LogInformation("发布评论成功：{CommentGuid}，推文={TweetGuid}", comment.CommentGuid, command.TweetGuid);
            return comment.CommentGuid;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "添加评论失败，推文: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
