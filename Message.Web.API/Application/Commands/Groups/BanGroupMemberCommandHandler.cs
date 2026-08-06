namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 封禁群组成员命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员。</para>
/// </summary>
public class BanGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<BanGroupMemberCommandHandler> logger) : IRequestHandler<BanGroupMemberCommand, bool>
{
    public async Task<bool> Handler(BanGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.BanMember))
            throw new UnauthorizedAccessException("仅群主或管理员可封禁成员");

        group.BanMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已被封禁", command.GroupId, command.UserId);
        return true;
    }
}
