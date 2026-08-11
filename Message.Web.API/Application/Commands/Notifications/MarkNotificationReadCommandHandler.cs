namespace Message.Web.API.Application.Commands.Notifications;

/// <summary>标记单条通知已读命令处理程序（R-04：校验归属，非本人视为无权）。</summary>
public class MarkNotificationReadCommandHandler(
    ITweetNotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarkNotificationReadCommandHandler> logger) : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public async Task<bool> Handler(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(command.NotifyGuid);
        if (notification == null)
            throw new KeyNotFoundException("通知不存在");

        // 越权防护：仅通知接收者可标记已读
        if (notification.UserGuid != command.UserId)
            throw new UnauthorizedAccessException("无权操作该通知");

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await notificationRepository.UpdateAsync(notification);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("通知 {NotifyGuid} 已标记为已读，用户={UserId}", command.NotifyGuid, command.UserId);
        return true;
    }
}
