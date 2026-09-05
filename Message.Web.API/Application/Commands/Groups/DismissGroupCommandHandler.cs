namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解散群组命令处理程序。
/// <para>权限（修复 S-04）：仅群主（Owner）可解散群组。</para>
/// <para>方案A：群聊会话的解散不再由此命令手工双写，改由 <see cref="GroupDissolvedEventHandler"/>
/// 监听 <c>GroupDissolvedEvent</c> 事件驱动完成（软删 IsDismissed，历史消息保留）。</para>
/// </summary>
public class DismissGroupCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<DismissGroupCommandHandler> logger) : IRequestHandler<DismissGroupCommand, bool>
{
    public async Task<bool> Handler(DismissGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.TransferOwnership))
            throw new UnauthorizedAccessException("仅群主可解散群组");

        group.Dismiss();
        await groupRepository.UpdateAsync(group);

        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 已解散", command.GroupId);
        return true;
    }
}
