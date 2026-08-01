namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 移除群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">要移除的用户 ID</param>
public record RemoveGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 移除群组成员命令处理程序。
/// </summary>
public class RemoveGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ILogger<RemoveGroupMemberCommandHandler> logger) : IRequestHandler<RemoveGroupMemberCommand, bool>
{
    public async Task<bool> Handler(RemoveGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.RemoveMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已从群组 {GroupId} 移除", command.UserId, command.GroupId);
        return true;
    }
}
