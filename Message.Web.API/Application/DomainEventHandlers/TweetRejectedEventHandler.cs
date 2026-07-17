using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Microsoft.Extensions.Logging;
using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetRejectedEventHandler(
    IEmailSender emailSender,
    ITweetNotificationRepository notificationRepository,
    ILogger<TweetRejectedEventHandler> logger) : INotificationHandler<TweetRejectedEvent>
{
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ITweetNotificationRepository _notificationRepository = notificationRepository;
    private readonly ILogger<TweetRejectedEventHandler> _logger = logger;

    public async Task Handler(TweetRejectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[{Time}] 推文审核驳回: TweetGuid={TweetGuid}, Reason={Reason}",
                DateTimeOffset.UtcNow, notification.TweetGuid, notification.Reason);

            var tweetNotification = TweetNotification.Create(
                notification.AuthorGuid,
                NotificationType.TweetRejected,
                "推文审核驳回",
                $"您的推文未通过审核，原因：{notification.Reason}",
                "Tweet",
                notification.TweetGuid);

            await _notificationRepository.AddAsync(tweetNotification);

            _logger.LogInformation("审核驳回通知已保存: NotifyGuid={NotifyGuid}, AuthorGuid={AuthorGuid}",
                tweetNotification.NotifyGuid, notification.AuthorGuid);

            _logger.LogInformation("邮件通知（占位）: 推文 {TweetGuid} 审核驳回，原因: {Reason}",
                notification.TweetGuid, notification.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理推文审核驳回事件失败: TweetGuid={TweetGuid}", notification.TweetGuid);
        }
    }
}
