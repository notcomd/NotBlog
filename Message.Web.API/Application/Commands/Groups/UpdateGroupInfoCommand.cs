namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 更新群组信息命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="GroupName">新的群名称</param>
/// <param name="Description">新的群描述（可为空）</param>
public record UpdateGroupInfoCommand(Guid GroupId, string GroupName, string? Description) : IRequest<bool>;

/// <summary>
/// 更新群组信息命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员，或群允许成员编辑群信息（AllowMemberEditInfo）。</para>
/// </summary>
public class UpdateGroupInfoCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<UpdateGroupInfoCommandHandler> logger) : IRequestHandler<UpdateGroupInfoCommand, bool>
{
    public async Task<bool> Handler(UpdateGroupInfoCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.EditGroupInfo))
            throw new UnauthorizedAccessException("无权限编辑群信息");

        group.UpdateGroupInfo(command.GroupName, command.Description, null);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 的信息已更新", command.GroupId);
        return true;
    }
}
