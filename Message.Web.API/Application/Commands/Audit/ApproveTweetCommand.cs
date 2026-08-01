namespace Message.Web.API.Application.Commands.Audit;

/// <summary>
/// 通过推文审核命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="AuditorGuid">审核人用户 ID</param>
public record ApproveTweetCommand(Guid TweetGuid, Guid AuditorGuid) : IRequest<bool>;

/// <summary>
/// 通过推文审核命令处理程序。
/// </summary>
public class ApproveTweetCommandHandler(
    ITweetRepository tweetRepository,
    ITweetAuditRepository auditRepository,
    ICurrentUserService currentUserService,
    ILogger<ApproveTweetCommandHandler> logger) : IRequestHandler<ApproveTweetCommand, bool>
{
    public async Task<bool> Handler(ApproveTweetCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始审核通过推文，ID: {TweetGuid}, 审核员: {AuditorGuid}", command.TweetGuid, command.AuditorGuid);

            if (!currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能审核推文");

            var tweet = await tweetRepository.GetByIdAsync(command.TweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            tweet.Approve(command.AuditorGuid);
            await tweetRepository.UpdateAsync(tweet);

            var auditLog = TweetAuditLog.Create(command.TweetGuid, command.AuditorGuid, AuditAction.Approve, "审核通过");
            await auditRepository.AddAsync(auditLog);

            await tweetRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("推文审核通过成功，ID: {TweetGuid}", command.TweetGuid);
            logger.LogInformation("推文审核通过：{TweetGuid}，审核人={AuditorGuid}", command.TweetGuid, command.AuditorGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "审核通过推文失败，ID: {TweetGuid}", command.TweetGuid);
            throw;
        }
    }
}
