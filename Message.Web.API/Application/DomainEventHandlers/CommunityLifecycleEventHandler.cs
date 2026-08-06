
namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 圈子生命周期与话题事件处理：统一发布事件总线（AI/MCP 出口）。
/// <para>监听：圈子创建/解散/转移/角色变更/话题创建，以及取消关注。</para>
/// </summary>
public class CommunityLifecycleEventHandler(
    ICommunityEventPublisher eventPublisher,
    ILogger<CommunityLifecycleEventHandler> logger) :
    INotificationHandler<CircleCreatedEvent>,
    INotificationHandler<CircleDissolvedEvent>,
    INotificationHandler<CircleOwnerTransferredEvent>,
    INotificationHandler<CircleMemberRoleChangedEvent>,
    INotificationHandler<TopicCreatedEvent>,
    INotificationHandler<UserUnfollowedEvent>
{
    public async Task Handler(CircleCreatedEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("circle.created", notification.OwnerGuid, notification.CircleGuid, null, notification, cancellationToken);

    public async Task Handler(CircleDissolvedEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("circle.dissolved", notification.OwnerGuid, notification.CircleGuid, null, notification, cancellationToken);

    public async Task Handler(CircleOwnerTransferredEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("circle.owner.transferred", notification.OldOwnerGuid, notification.CircleGuid, notification.NewOwnerGuid, notification, cancellationToken);

    public async Task Handler(CircleMemberRoleChangedEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("circle.member.role_changed", notification.OperatorGuid, notification.CircleGuid, notification.UserGuid, notification, cancellationToken);

    public async Task Handler(TopicCreatedEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("topic.created", notification.CreatorGuid, null, notification.TopicGuid, notification, cancellationToken);

    public async Task Handler(UserUnfollowedEvent notification, CancellationToken cancellationToken = default)
        => await PublishAsync("user.unfollowed", notification.FollowerGuid, null, notification.FolloweeGuid, notification, cancellationToken);

    private async Task PublishAsync(string eventType, Guid actorGuid, Guid? circleGuid, Guid? targetGuid, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await eventPublisher.PublishAsync(
                CommunityEventEnvelope.Create(eventType, actorGuid, circleGuid, targetGuid, payload), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "社区生命周期事件发布失败: Type={EventType}", eventType);
        }
    }
}
