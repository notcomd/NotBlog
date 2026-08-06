namespace Message.Web.API.Application.Commands.Audit;
/// <summary>
/// 处理举报命令处理程序。
/// </summary>
public class ResolveReportCommandHandler(
    ITweetReportRepository reportRepository,
    ITweetRepository tweetRepository,
    ICommentRepository commentRepository,
    ICurrentUserService currentUserService,
    ILogger<ResolveReportCommandHandler> logger) : IRequestHandler<ResolveReportCommand, bool>
{
    public async Task<bool> Handler(ResolveReportCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var note = command.Note ?? string.Empty;

            logger.LogInformation("开始处理举报，ID: {ReportGuid}, 审核员: {ReviewerGuid}", command.ReportGuid, command.ReviewerGuid);

            if (!currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能处理举报");

            var report = await reportRepository.GetByIdAsync(command.ReportGuid);
            if (report == null)
                throw new KeyNotFoundException("举报不存在");

            report.StartReview(command.ReviewerGuid);

            if (command.IsContentRemoved)
            {
                report.ResolveRemoved(command.ReviewerGuid, note);

                // 删除被举报的内容
                if (report.TargetType == ReportTargetType.Tweet)
                {
                    var tweet = await tweetRepository.GetByIdAsync(report.TargetGuid);
                    if (tweet != null)
                    {
                        await tweetRepository.DeleteAsync(report.TargetGuid);
                        logger.LogInformation("已删除被举报推文，ID: {TargetGuid}", report.TargetGuid);
                    }
                }
                else if (report.TargetType == ReportTargetType.Comment)
                {
                    var comment = await commentRepository.GetByIdAsync(report.TargetGuid);
                    if (comment != null && !comment.IsDeleted)
                    {
                        await commentRepository.DeleteAsync(report.TargetGuid);
                        logger.LogInformation("已删除被举报评论，ID: {TargetGuid}", report.TargetGuid);
                    }
                }
            }
            else
            {
                report.ResolveRejected(command.ReviewerGuid, note);
            }

            await reportRepository.UpdateAsync(report);
            await reportRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("举报处理成功，ID: {ReportGuid}", command.ReportGuid);
            logger.LogInformation("举报已处理：{ReportGuid}，审核人={ReviewerGuid}，删除内容={IsContentRemoved}",
                command.ReportGuid, command.ReviewerGuid, command.IsContentRemoved);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "处理举报失败，ID: {ReportGuid}", command.ReportGuid);
            throw;
        }
    }
}
