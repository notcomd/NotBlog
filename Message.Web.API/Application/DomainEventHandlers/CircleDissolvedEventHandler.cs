namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 社区解散事件处理：事件驱动同步解散对应的社区聊天会话（ChatSession.Dismiss），
/// 与群解散处理（GroupDissolvedEventHandler）同模式，确保「社区解散 ⇔ 会话解散」数据一致。
/// </summary>
public class CircleDissolvedEventHandler(
    IChatSessionRepository sessionRepository,
    ILogger<CircleDissolvedEventHandler> logger) : INotificationHandler<CircleDissolvedEvent>
{
    public async Task Handler(CircleDissolvedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // 事件驱动：同步解散社区聊天会话（不存在或已解散则跳过）
            var session = await sessionRepository.GetByCircleIdAsync(notification.CircleGuid);
            if (session is not null && !session.IsDismissed)
            {
                session.Dismiss();
                await sessionRepository.UpdateAsync(session);
            }

            logger.LogInformation("社区 {CircleGuid} 已解散，社区聊天会话已同步解散", notification.CircleGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理社区解散事件失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
