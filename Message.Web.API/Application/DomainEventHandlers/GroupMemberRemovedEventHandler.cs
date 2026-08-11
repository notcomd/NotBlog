namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 成员被移出群事件处理（R-11）：向被移除成员写入站内通知。
/// <para>通知经 AddAsync 加入当前 DbContext，随触发事件的事务一并落库。</para>
/// </summary>
public class GroupMemberRemovedEventHandler(
    IGroupRepository groupRepository,
    ITweetNotificationRepository notificationRepository,
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

            logger.LogInformation("用户 {UserId} 被移出群 {GroupId}，通知已写入", notification.UserId, notification.GroupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理移出成员事件失败: GroupId={GroupId}, UserId={UserId}",
                notification.GroupId, notification.UserId);
        }
    }
}
