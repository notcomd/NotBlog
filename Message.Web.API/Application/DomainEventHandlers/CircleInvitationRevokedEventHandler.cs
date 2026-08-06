
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 邀请撤销事件处理：发布事件总线（AI/MCP 出口）。
/// </summary>
public class CircleInvitationRevokedEventHandler(
    ICommunityEventPublisher eventPublisher,
    ILogger<CircleInvitationRevokedEventHandler> logger) : INotificationHandler<CircleInvitationRevokedEvent>
{
    public async Task Handler(CircleInvitationRevokedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.invitation.revoked", notification.OperatorGuid,
                notification.CircleGuid, notification.InviteGuid, notification), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邀请撤销事件处理失败: Invite={InviteGuid}", notification.InviteGuid);
        }
    }
}
