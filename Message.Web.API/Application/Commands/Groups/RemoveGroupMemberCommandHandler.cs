namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 移除群组成员命令处理程序。
/// <para>权限（修复 S-04）：普通成员仅可移除自己（退出群组）；移除他人需群主/管理员。</para>
/// <para>联动（2026-08-15）：成员退出群组后同步移出群聊会话（ChatSession.Participants；
/// 群聊会话无最少人数限制，私聊会话不受此路径影响）。</para>
/// </summary>
public class RemoveGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    IChatSessionRepository sessionRepository,
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

        // 联动：同步移出群聊会话（会话不存在则跳过）
        var session = await sessionRepository.GetByGroupIdAsync(group.GroupId);
        if (session is not null && session.IsParticipant(command.UserId))
        {
            session.RemoveParticipant(command.UserId);
            await sessionRepository.UpdateAsync(session);
        }

        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已从群组 {GroupId} 移除", command.UserId, command.GroupId);
        return true;
    }
}
