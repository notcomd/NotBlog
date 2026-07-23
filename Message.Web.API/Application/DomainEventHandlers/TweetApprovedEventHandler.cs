using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetApprovedEventHandler(
    IEmailSender emailSender,
    ITweetNotificationRepository notificationRepository,
    ILogger<TweetApprovedEventHandler> logger) : INotificationHandler<TweetApprovedEvent>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ITweetNotificationRepository _notificationRepository = notificationRepository;
    private readonly ILogger<TweetApprovedEventHandler> _logger = logger;

    public async Task Handler(TweetApprovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[{Time}] 推文审核通过: TweetGuid={TweetGuid}",
                DateTimeOffset.UtcNow, notification.TweetGuid);

            var tweetNotification = TweetNotification.Create(
                notification.AuthorGuid,
                NotificationType.TweetApproved,
                "推文审核通过",
                "您的推文已通过审核",
                "Tweet",
                notification.TweetGuid);

            await _notificationRepository.AddAsync(tweetNotification);

            _logger.LogInformation("审核通过通知已保存: NotifyGuid={NotifyGuid}, AuthorGuid={AuthorGuid}",
                tweetNotification.Id, notification.AuthorGuid);

            _logger.LogInformation("邮件通知（占位）: 推文 {TweetGuid} 审核通过，待后续集成邮件模板后发送",
                notification.TweetGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理推文审核通过事件失败: TweetGuid={TweetGuid}", notification.TweetGuid);
        }
    }
}
