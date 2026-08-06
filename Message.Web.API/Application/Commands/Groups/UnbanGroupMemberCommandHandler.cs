namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解除群组成员封禁命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员。</para>
/// </summary>
public class UnbanGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<UnbanGroupMemberCommandHandler> logger) : IRequestHandler<UnbanGroupMemberCommand, bool>
{
    public async Task<bool> Handler(UnbanGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.BanMember))
            throw new UnauthorizedAccessException("仅群主或管理员可解除封禁");

        group.UnbanMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已解除封禁", command.GroupId, command.UserId);
        return true;
    }
}
