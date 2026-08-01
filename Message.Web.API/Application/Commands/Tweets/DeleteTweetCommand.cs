namespace Message.Web.API.Application.Commands.Tweets;

/// <summary>
/// 删除推文命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="UserId">作者用户 ID</param>
public record DeleteTweetCommand(Guid TweetGuid, Guid UserId) : IRequest<bool>;

/// <summary>
/// 删除推文命令处理程序。
/// </summary>
public class DeleteTweetCommandHandler(
    ITweetRepository tweetRepository,
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
                throw new UnauthorizedAccessException("无权删除此推文");

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
