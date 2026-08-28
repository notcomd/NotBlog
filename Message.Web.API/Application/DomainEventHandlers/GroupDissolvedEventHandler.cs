namespace Message.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 群解散事件处理（R-11）：向群内所有成员写入站内通知。
/// <para>方案A：群解散态由事件驱动同步——解散对应群聊会话（ChatSession.Dismiss），
/// 替代命令层手工双写，确保「群解散 ⇔ 会话解散」数据一致。</para>
/// <para>通知经 AddAsync 加入当前 DbContext，随触发事件的事务一并落库（与 TweetApprovedEventHandler 同模式）。</para>
/// </summary>
public class GroupDissolvedEventHandler(
    IGroupRepository groupRepository,
    ITweetNotificationRepository notificationRepository,
    IChatSessionRepository sessionRepository,
    ILogger<GroupDissolvedEventHandler> logger) : INotificationHandler<GroupDissolvedEvent>
{
    public async Task Handler(GroupDissolvedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var group = await groupRepository.GetByIdWithMembersAsync(notification.GroupId);
            if (group is null)
            {
                logger.LogWarning("群解散事件处理时群不存在: GroupId={GroupId}", notification.GroupId);
                return;
            }

            // 通知全部成员（含解散操作者本人，语义为全员知晓）
            foreach (var member in group.Members)
            {
                await notificationRepository.AddAsync(TweetNotification.Create(
                    member.UserId, NotificationType.GroupDissolved,
                    "群解散通知", $"您所在的群「{group.GroupName}」已解散", "Group", group.GroupId));
            }

            // 事件驱动：同步解散对应群聊会话（不存在则跳过，无解散态冗余）
            var session = await sessionRepository.GetByGroupIdAsync(group.GroupId);
            if (session is not null && !session.IsDismissed)
            {
                session.Dismiss();
                await sessionRepository.UpdateAsync(session);
            }

            logger.LogInformation("群 {GroupId} 解散通知已写入 {Count} 条", group.GroupId, group.Members.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理群解散事件失败: GroupId={GroupId}", notification.GroupId);
        }
    }
}
