namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 解散群组命令处理程序。
/// <para>权限（修复 S-04）：仅群主（Owner）可解散群组。</para>
/// <para>联动（2026-08-15）：群组解散后同步解散关联的群聊会话（软删 IsDismissed，历史消息保留）。</para>
/// </summary>
public class DismissGroupCommandHandler(
    IGroupRepository groupRepository,
    IChatSessionRepository sessionRepository,
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

        // 联动：同步解散群聊会话（软删，历史消息保留）
        var session = await sessionRepository.GetByGroupIdAsync(group.GroupId);
        if (session is not null && !session.IsDismissed)
        {
            session.Dismiss();
            await sessionRepository.UpdateAsync(session);
        }

        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 已解散", command.GroupId);
        return true;
    }
}
