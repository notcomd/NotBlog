namespace Message.Web.API.Application.Commands.Reports;

/// <summary>
/// 提交举报命令。
/// </summary>
/// <param name="UserId">举报者用户 ID</param>
/// <param name="TargetType">举报对象类型（Tweet/Comment）</param>
/// <param name="TargetGuid">举报对象 ID</param>
/// <param name="Reason">举报原因</param>
/// <param name="Category">举报分类</param>
/// <param name="EvidenceUrls">证据 URL 集合</param>
public record SubmitReportCommand(
    Guid UserId,
    string TargetType,
    Guid TargetGuid,
    string Reason,
    string Category,
    IEnumerable<string>? EvidenceUrls) : IRequest<bool>;

/// <summary>
/// 提交举报命令处理程序。
/// </summary>
public class SubmitReportCommandHandler(
    ITweetReportRepository reportRepository,
    ITweetRepository tweetRepository,
    ICommentRepository commentRepository,
    ILogger<SubmitReportCommandHandler> logger) : IRequestHandler<SubmitReportCommand, bool>
{
    public async Task<bool> Handler(SubmitReportCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var parsedTargetType = Enum.Parse<ReportTargetType>(command.TargetType);
            var parsedCategory = Enum.Parse<ReportCategory>(command.Category);

            logger.LogInformation("开始提交举报，举报人: {ReporterGuid}, 类型: {TargetType}, 目标: {TargetGuid}",
                command.UserId, parsedTargetType, command.TargetGuid);

            // 验证目标存在并获取被举报用户ID
            Guid reportedUserGuid;
            if (parsedTargetType == ReportTargetType.Tweet)
            {
                var tweet = await tweetRepository.GetByIdAsync(command.TargetGuid);
                if (tweet == null)
                    throw new KeyNotFoundException("被举报的推文不存在");
                reportedUserGuid = tweet.AuthorGuid;
            }
            else if (parsedTargetType == ReportTargetType.Comment)
            {
                var comment = await commentRepository.GetByIdAsync(command.TargetGuid);
                if (comment == null)
                    throw new KeyNotFoundException("被举报的评论不存在");
                reportedUserGuid = comment.UserGuid;
            }
            else
            {
                throw new ArgumentException("不支持的举报目标类型");
            }

            var report = TweetReport.Create(command.UserId, parsedTargetType, command.TargetGuid, reportedUserGuid,
                command.Reason, parsedCategory, command.EvidenceUrls);

            await reportRepository.AddAsync(report);
            await reportRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("举报提交成功，ID: {ReportGuid}", report.ReportGuid);
            logger.LogInformation("提交举报成功：目标类型={TargetType}，目标={TargetGuid}",
                command.TargetType, command.TargetGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            logger.LogError(ex, "提交举报失败，举报人: {ReporterGuid}", command.UserId);
            throw;
        }
    }
}
