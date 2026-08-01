namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 封禁群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被封禁的用户 ID</param>
public record BanGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 封禁群组成员命令处理程序。
/// </summary>
public class BanGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ILogger<BanGroupMemberCommandHandler> logger) : IRequestHandler<BanGroupMemberCommand, bool>
{
    public async Task<bool> Handler(BanGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.BanMember(command.UserId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已被封禁", command.GroupId, command.UserId);
        return true;
    }
}
