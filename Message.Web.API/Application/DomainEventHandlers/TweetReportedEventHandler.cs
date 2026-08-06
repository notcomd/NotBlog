
namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetReportedEventHandler(ILogger<TweetReportedEventHandler> logger) : INotificationHandler<TweetReportedEvent>
{
    private readonly ILogger<TweetReportedEventHandler> _logger = logger;

    public async Task Handler(TweetReportedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "处理举报事件: ReportGuid={ReportGuid}, ReporterGuid={ReporterGuid}, TargetType={TargetType}, TargetGuid={TargetGuid}",
                notification.ReportGuid, notification.ReporterGuid, notification.TargetType, notification.TargetGuid);

            _logger.LogInformation("[{Time}] 收到举报: ReportGuid={ReportGuid}, ReporterGuid={ReporterGuid}, TargetType={TargetType}, TargetGuid={TargetGuid}, Reason={Reason}",
                DateTimeOffset.UtcNow, notification.ReportGuid, notification.ReporterGuid, notification.TargetType, notification.TargetGuid, notification.Reason);

            // Future: 通知管理员有新举报待处理
            _logger.LogInformation("举报已记录，待管理员处理: ReportGuid={ReportGuid}", notification.ReportGuid);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理举报事件失败: ReportGuid={ReportGuid}", notification.ReportGuid);
        }
    }
}
