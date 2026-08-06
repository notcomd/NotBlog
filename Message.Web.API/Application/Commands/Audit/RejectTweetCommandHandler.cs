namespace Message.Web.API.Application.Commands.Audit;
/// <summary>
/// 驳回推文命令处理程序。
/// </summary>
public class RejectTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetAuditRepository auditRepository,
    ICurrentUserService currentUserService,
    ILogger<RejectTweetCommandHandler> logger) : IRequestHandler<RejectTweetCommand, bool>
{
    public async Task<bool> Handler(RejectTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var reason = command.Reason ?? string.Empty;

            logger.LogInformation("开始驳回推文，ID: {TweetGuid}, 审核员: {AuditorGuid}, 原因: {Reason}",
                command.TweetGuid, command.AuditorGuid, reason);

            if (!currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能审核推文");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            tweet.Reject(command.AuditorGuid, reason);
            await tweetRepository.UpdateAsync(tweet);

            var auditLog = TweetAuditLog.Create(command.TweetGuid, command.AuditorGuid, AuditAction.Reject, reason);
            await auditRepository.AddAsync(auditLog);

            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文驳回成功，ID: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("驳回推文成功：{TweetGuid}，审核人={AuditorGuid}", command.TweetGuid, command.AuditorGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "驳回推文失败，ID: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
