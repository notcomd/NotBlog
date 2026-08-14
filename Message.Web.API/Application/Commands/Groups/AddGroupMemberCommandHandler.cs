namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 添加群组成员命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员，或群允许成员邀请（AllowMemberInvite）。</para>
/// <para>联动（2026-08-15）：成员加入群组后同步加入群聊会话（ChatSession.Participants）；
/// 若会话缺失（历史数据异常）则按当前群成员自动重建，保证群组与会话链条完整。</para>
/// </summary>
public class AddGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    ILogger<AddGroupMemberCommandHandler> logger) : IRequestHandler<AddGroupMemberCommand, bool>
{
    public async Task<bool> Handler(AddGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.InviteMember))
            throw new UnauthorizedAccessException("无权限邀请成员");

        group.AddMember(command.UserId, command.Role);
        await groupRepository.UpdateAsync(group);

        // 联动：同步加入群聊会话（缺失则按当前群成员重建）
        var session = await sessionRepository.GetByGroupIdAsync(group.GroupId);
        if (session is null)
        {
            session = ChatSession.CreateGroupSession(
                group.GroupId, group.OwnerId, group.GroupName,
                group.Members.Select(m => m.UserId).ToHashSet());
            await sessionRepository.AddAsync(session);
        }
        else if (!session.IsParticipant(command.UserId))
        {
            session.AddParticipant(command.UserId);
            await sessionRepository.UpdateAsync(session);
        }

        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已加入群组 {GroupId}，角色 {Role}",
            command.UserId, command.GroupId, command.Role);
        return true;
    }
}
