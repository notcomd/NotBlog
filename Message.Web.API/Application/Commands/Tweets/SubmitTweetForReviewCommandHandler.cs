namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 提交推文审核命令处理程序（作者本人提交草稿进入待审核）。
/// <para>
/// 语义：仅作者本人可提交；推文须为草稿状态（Draft → Pending）。
/// 圈子帖已是 Approved（免审核），调用会因状态非 Draft 抛 <see cref="InvalidOperationException"/>，由端点转为 400。
/// </para>
/// </summary>
public class SubmitTweetForReviewCommandHandler(
    ITweetRepository tweetRepository,
    ILogger<SubmitTweetForReviewCommandHandler> logger) : IRequestHandler<SubmitTweetForReviewCommand, bool>
{
    public async Task<bool> Handler(SubmitTweetForReviewCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始提交推文审核，ID: {TweetGuid}", command.TweetGuid);

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.AuthorGuid != command.UserId)
                throw new UnauthorizedAccessException("无权提交此推文");

            // 状态校验由领域方法负责：非草稿（含圈子帖 Approved）抛 InvalidOperationException，端点转为 400。
            tweet.Publish();
            await tweetRepository.UpdateAsync(tweet);
            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文已提交审核，ID: {TweetGuid}", command.TweetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "提交推文审核失败，ID: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
