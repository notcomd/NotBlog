namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 解除群组成员禁言命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被解除禁言的用户 ID</param>
public record UnmuteGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 解除群组成员禁言命令处理程序。
/// </summary>
public class UnmuteGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ILogger<UnmuteGroupMemberCommandHandler> logger) : IRequestHandler<UnmuteGroupMemberCommand, bool>
{
    public async Task<bool> Handler(UnmuteGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UnmuteMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已解除禁言", command.GroupId, command.UserId);
        return true;
    }
}
