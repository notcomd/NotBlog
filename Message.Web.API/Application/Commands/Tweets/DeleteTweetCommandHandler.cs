namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 删除推文命令处理程序。
/// <para>权限：作者本人 / 全局管理员 / 频道（圈子）帖的圈主或圈管理员（频道内容管理）。</para>
/// </summary>
public class DeleteTweetCommandHandler(
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteTweetCommandHandler> logger) : IRequestHandler<DeleteTweetCommand, bool>
{
    public async Task<bool> Handler(DeleteTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始删除推文，ID: {TweetGuid}", command.TweetGuid);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != command.UserId && !currentUserService.IsAdmin())
            {
                // 频道内容管理：圈子帖允许圈主/圈管理员删除成员帖子
                if (tweet.CircleGuid is null)
                    throw new UnauthorizedAccessException("无权删除此推文");
                var member = await circleRepository.GetMemberAsync(tweet.CircleGuid.Value, command.UserId);
                if (member is null || member.Status != CircleMemberStatus.Active
                    || (member.Role != CircleMemberRole.Owner && member.Role != CircleMemberRole.Admin))
                    throw new UnauthorizedAccessException("无权删除此推文");
            }

            await tweetRepository.DeleteAsync(command.TweetGuid);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文删除成功，ID: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("删除推文成功：{TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "删除推文失败，ID: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
