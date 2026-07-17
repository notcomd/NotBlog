using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.IRepository;
using Microsoft.Extensions.Logging;
using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class CommentAddedEventHandler : INotificationHandler<CommentAddedEvent>
{
    private readonly ITweetNotificationRepository _notificationRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILogger<CommentAddedEventHandler> _logger;

    public CommentAddedEventHandler(
        ITweetNotificationRepository notificationRepository,
        ICommentRepository commentRepository,
        ILogger<CommentAddedEventHandler> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handler(CommentAddedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[{Time}] 评论通知处理: CommentGuid={CommentGuid}, TweetGuid={TweetGuid}, UserGuid={UserGuid}",
                DateTimeOffset.UtcNow, notification.CommentGuid, notification.TweetGuid, notification.UserGuid);

            // 如果有父评论（回复场景），通知被回复的用户
            if (notification.ParentGuid.HasValue)
            {
                var parentComment = await _commentRepository.GetByIdAsync(notification.ParentGuid.Value);
                if (parentComment is not null && parentComment.UserGuid != notification.UserGuid)
                {
                    var notify = TweetNotification.Create(
                        parentComment.UserGuid,
                        NotificationType.CommentReplied,
                        "评论回复提醒",
                        $"有人回复了你的评论",
                        "Comment",
                        notification.CommentGuid);

                    await _notificationRepository.AddAsync(notify);
                }
            }

            _logger.LogInformation("[{Time}] 评论通知处理完成: CommentGuid={CommentGuid}",
                DateTimeOffset.UtcNow, notification.CommentGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] 评论通知处理异常: CommentGuid={CommentGuid}",
                DateTimeOffset.UtcNow, notification.CommentGuid);
        }
    }
}
