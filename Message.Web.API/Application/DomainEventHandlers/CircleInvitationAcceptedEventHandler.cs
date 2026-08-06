
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 邀请接受事件处理：发布事件总线（AI/MCP 出口）。
/// </summary>
public class CircleInvitationAcceptedEventHandler(
    ICommunityEventPublisher eventPublisher,
    ILogger<CircleInvitationAcceptedEventHandler> logger) : INotificationHandler<CircleInvitationAcceptedEvent>
{
    public async Task Handler(CircleInvitationAcceptedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.invitation.accepted", notification.UserGuid,
                notification.CircleGuid, notification.InviteGuid, notification), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邀请接受事件处理失败: Invite={InviteGuid}", notification.InviteGuid);
        }
    }
}
