using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class ReportResolvedEventHandler : INotificationHandler<ReportResolvedEvent>
{
    private readonly ITweetNotificationRepository _notificationRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReportResolvedEventHandler> _logger;

    public ReportResolvedEventHandler(
        ITweetNotificationRepository notificationRepository,
        IEmailSender emailSender,
        ILogger<ReportResolvedEventHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Handler(ReportResolvedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "处理举报结果事件: ReportGuid={ReportGuid}, ResultStatus={ResultStatus}, ReporterGuid={ReporterGuid}, ReportedUserGuid={ReportedUserGuid}",
                notification.ReportGuid, notification.ResultStatus, notification.ReporterGuid, notification.ReportedUserGuid);

            _logger.LogInformation("[{Time}] 举报处理结果: ReportGuid={ReportGuid}, ResultStatus={ResultStatus}, Note={Note}",
                DateTimeOffset.UtcNow, notification.ReportGuid, notification.ResultStatus, notification.Note);

            // 通知举报者
            var resultText = notification.ResultStatus switch
            {
                ReportStatus.Resolved_Removed => "被举报内容已下架",
                ReportStatus.Resolved_Rejected => "举报未被采纳",
                _ => "举报已处理"
            };

            var reporterContent = $"您提交的举报已处理，处理结果：{resultText}。处理备注：{notification.Note}";
            var reporterNotification = TweetNotification.Create(
                notification.ReporterGuid,
                notification.ResultStatus == ReportStatus.Resolved_Removed
                    ? NotificationType.ReportResolved_Removed
                    : NotificationType.ReportResolved_Rejected,
                "举报处理结果",
                reporterContent,
                "Report",
                notification.ReportGuid);

            await _notificationRepository.AddAsync(reporterNotification);

            _logger.LogInformation("举报者通知已保存: NotifyGuid={NotifyGuid}, ReporterGuid={ReporterGuid}",
                reporterNotification.Id, notification.ReporterGuid);

            // 如果内容被下架，同时通知被举报者
            if (notification.ResultStatus == ReportStatus.Resolved_Removed)
            {
                var reportedContent = $"您的内容已被下架。处理备注：{notification.Note}";
                var reportedNotification = TweetNotification.Create(
                    notification.ReportedUserGuid,
                    NotificationType.ReportResolved_Removed,
                    "内容下架通知",
                    reportedContent,
                    "Report",
                    notification.ReportGuid);

                await _notificationRepository.AddAsync(reportedNotification);

                _logger.LogInformation("被举报者通知已保存: NotifyGuid={NotifyGuid}, ReportedUserGuid={ReportedUserGuid}",
                    reportedNotification.Id, notification.ReportedUserGuid);
            }

            // 发送邮件（异步，火后即忘，带 try-catch 保护）
            _ = SendEmailsSafelyAsync(notification, resultText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理举报结果事件失败: ReportGuid={ReportGuid}", notification.ReportGuid);
        }
    }

    private async Task SendEmailsSafelyAsync(ReportResolvedEvent notification, string resultText)
    {
        try
        {
            var reporterEmail = $"{notification.ReporterGuid}@notblog.local";
            var reporterSubject = "举报处理结果通知";
            var reporterBody = $"您提交的举报已处理，处理结果：{resultText}。处理备注：{notification.Note}";

            await _emailSender.SendEmailAsync(reporterEmail, reporterSubject, reporterBody);

            _logger.LogInformation("举报者邮件已发送: ReporterGuid={ReporterGuid}, ReportGuid={ReportGuid}",
                notification.ReporterGuid, notification.ReportGuid);

            if (notification.ResultStatus == ReportStatus.Resolved_Removed)
            {
                var reportedEmail = $"{notification.ReportedUserGuid}@notblog.local";
                var reportedSubject = "内容下架通知";
                var reportedBody = $"您的内容已被下架。处理备注：{notification.Note}";

                await _emailSender.SendEmailAsync(reportedEmail, reportedSubject, reportedBody);

                _logger.LogInformation("被举报者邮件已发送: ReportedUserGuid={ReportedUserGuid}, ReportGuid={ReportGuid}",
                    notification.ReportedUserGuid, notification.ReportGuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送举报处理结果邮件失败: ReportGuid={ReportGuid}", notification.ReportGuid);
        }
    }
}
