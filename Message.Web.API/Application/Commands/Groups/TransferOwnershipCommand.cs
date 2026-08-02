namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 转让群主命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="NewOwnerId">新群主用户 ID</param>
public record TransferOwnershipCommand(Guid GroupId, Guid NewOwnerId) : IRequest<bool>;

/// <summary>
/// 转让群主命令处理程序。
/// <para>权限（修复 S-04）：仅群主（Owner）可转让群主。</para>
/// </summary>
public class TransferOwnershipCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<TransferOwnershipCommandHandler> logger) : IRequestHandler<TransferOwnershipCommand, bool>
{
    public async Task<bool> Handler(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.TransferOwnership))
            throw new UnauthorizedAccessException("仅群主可转让群主");

        group.TransferOwnership(command.NewOwnerId);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 的群主已转让给用户 {NewOwnerId}",
            command.GroupId, command.NewOwnerId);
        return true;
    }
}
