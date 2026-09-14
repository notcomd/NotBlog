
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员加入圈子事件处理：实时推送 circle:{id} 频道 + 事件总线 + 同步加入社区群组与聊天会话参与者。
/// </summary>
public class CircleMemberJoinedEventHandler(
    CommunityDeliveryService deliveryService,
    ICommunityEventPublisher eventPublisher,
    IGroupRepository groupRepository,
    IChatSessionRepository sessionRepository,
    ILogger<CircleMemberJoinedEventHandler> logger) : INotificationHandler<CircleMemberJoinedEvent>
{
    public async Task Handler(CircleMemberJoinedEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await deliveryService.PushMemberJoinedAsync(notification.CircleGuid, notification.UserGuid, notification.Role.ToString());

            await eventPublisher.PublishAsync(CommunityEventEnvelope.Create(
                "circle.member.joined", notification.UserGuid,
                notification.CircleGuid, null, notification), cancellationToken);

            // 事件驱动：同步加入社区群组（社区聊天以真群组承载；存量社区无群组则跳过）
            var group = await groupRepository.GetByCircleIdWithMembersAsync(notification.CircleGuid);
            if (group is not null && !group.IsMember(notification.UserGuid))
            {
                group.AddMember(notification.UserGuid);
                await groupRepository.UpdateAsync(group);
            }

            // 事件驱动：同步加入社区聊天会话（会话不存在或已在会话中则跳过）
            var session = await sessionRepository.GetByCircleIdAsync(notification.CircleGuid);
            if (session is not null && !session.IsParticipant(notification.UserGuid))
            {
                session.AddParticipant(notification.UserGuid);
                await sessionRepository.UpdateAsync(session);
            }

            logger.LogInformation("成员加入圈子已推送: Circle={CircleGuid}, User={UserGuid}", notification.CircleGuid, notification.UserGuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "成员加入圈子事件处理失败: Circle={CircleGuid}", notification.CircleGuid);
        }
    }
}
