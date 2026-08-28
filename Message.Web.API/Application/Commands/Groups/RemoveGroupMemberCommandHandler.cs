namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 移除群组成员命令处理程序。
/// <para>权限（修复 S-04）：普通成员仅可移除自己（退出群组）；移除他人需群主/管理员。</para>
/// <para>方案A：群聊会话成员的同步不再手工双写，改由 <see cref="GroupMemberRemovedEventHandler"/>
/// 监听 <c>GroupMemberRemovedEvent</c> 事件驱动完成，命令层保持单一职责。</para>
/// </summary>
public class RemoveGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<RemoveGroupMemberCommandHandler> logger) : IRequestHandler<RemoveGroupMemberCommand, bool>
{
    public async Task<bool> Handler(RemoveGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        var isSelfExit = command.UserId == operatorId;
        if (!isSelfExit && !group.HasPermission(operatorId, GroupPermission.RemoveMember))
            throw new UnauthorizedAccessException("仅群主或管理员可移除成员");

        group.RemoveMember(command.UserId);
        await groupRepository.UpdateAsync(group);

        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已从群组 {GroupId} 移除", command.UserId, command.GroupId);
        return true;
    }
}
