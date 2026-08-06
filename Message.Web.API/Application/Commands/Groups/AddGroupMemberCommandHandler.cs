namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 添加群组成员命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员，或群允许成员邀请（AllowMemberInvite）。</para>
/// </summary>
public class AddGroupMemberCommandHandler(
    IGroupRepository groupRepository,
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
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已加入群组 {GroupId}，角色 {Role}",
            command.UserId, command.GroupId, command.Role);
        return true;
    }
}
