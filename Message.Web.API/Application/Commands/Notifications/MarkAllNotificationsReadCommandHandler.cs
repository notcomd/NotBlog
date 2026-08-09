namespace Message.Web.API.Application.Commands.Notifications;

/// <summary>全部通知标记已读命令处理程序（R-04）。</summary>
public class MarkAllNotificationsReadCommandHandler(
    ITweetNotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarkAllNotificationsReadCommandHandler> logger) : IRequestHandler<MarkAllNotificationsReadCommand, bool>
{
    public async Task<bool> Handler(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadAsync(command.UserId);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 的全部通知已标记为已读", command.UserId);
        return true;
    }
}
