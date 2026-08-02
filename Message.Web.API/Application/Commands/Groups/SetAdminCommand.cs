namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 设置/取消群组成员管理员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">目标用户 ID</param>
/// <param name="IsAdmin">true 设为管理员，false 取消管理员</param>
public record SetAdminCommand(Guid GroupId, Guid UserId, bool IsAdmin) : IRequest<bool>;

/// <summary>
/// 设置/取消群组成员管理员命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员。</para>
/// </summary>
public class SetAdminCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<SetAdminCommandHandler> logger) : IRequestHandler<SetAdminCommand, bool>
{
    public async Task<bool> Handler(SetAdminCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.RemoveMember))
            throw new UnauthorizedAccessException("仅群主或管理员可设置管理员");

        if (command.IsAdmin)
        {
            group.PromoteMember(command.UserId);
            await groupRepository.UpdateAsync(group);
            await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            group.DemoteMember(command.UserId);
            await groupRepository.UpdateAsync(group);
            await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 的管理员状态已设置为 {IsAdmin}",
            command.GroupId, command.UserId, command.IsAdmin);
        return true;
    }
}
