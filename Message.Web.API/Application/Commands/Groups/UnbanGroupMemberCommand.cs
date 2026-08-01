namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 解除群组成员封禁命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被解除封禁的用户 ID</param>
public record UnbanGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 解除群组成员封禁命令处理程序。
/// </summary>
public class UnbanGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ILogger<UnbanGroupMemberCommandHandler> logger) : IRequestHandler<UnbanGroupMemberCommand, bool>
{
    public async Task<bool> Handler(UnbanGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UnbanMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已解除封禁", command.GroupId, command.UserId);
        return true;
    }
}
