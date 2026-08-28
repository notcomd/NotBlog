namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员被移出群事件处理（R-11）：
/// <list type="bullet">
/// <item>向被移除成员写入站内通知。</item>
/// <item>方案A：同步将该成员移出对应群聊会话（ChatSession.RemoveParticipant），
/// 替代 RemoveGroupMemberCommandHandler 中的手工双写。</item>
/// </list>
/// <para>通知经 AddAsync 加入当前 DbContext，随触发事件的事务一并落库。</para>
/// </summary>
public class GroupMemberRemovedEventHandler(
    IGroupRepository groupRepository,
    ITweetNotificationRepository notificationRepository,
    IChatSessionRepository sessionRepository,
    ILogger<GroupMemberRemovedEventHandler> logger) : INotificationHandler<GroupMemberRemovedEvent>
{
    public async Task Handler(GroupMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupRepository.GetByIdAsync(notification.GroupId);
            if (group is null)
            {
                logger.LogWarning("移出成员事件处理时群不存在: GroupId={GroupId}", notification.GroupId);
                return;
            }

            await notificationRepository.AddAsync(TweetNotification.Create(
                notification.UserId, NotificationType.GroupMemberRemoved,
                "移出群通知", $"您已被移出群「{group.GroupName}」", "Group", group.GroupId));

            // 事件驱动：同步移出群聊会话（会话不存在或成员不在会话中则跳过）
            var session = await sessionRepository.GetByGroupIdAsync(group.GroupId);
            if (session is not null && session.IsParticipant(notification.UserId))
            {
                session.RemoveParticipant(notification.UserId);
                await sessionRepository.UpdateAsync(session);
            }

            logger.LogInformation("用户 {UserId} 被移出群 {GroupId}，通知已写入", notification.UserId, notification.GroupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理移出成员事件失败: GroupId={GroupId}, UserId={UserId}",
                notification.GroupId, notification.UserId);
        }
    }
}
