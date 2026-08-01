namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 解散群组命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record DismissGroupCommand(Guid GroupId) : IRequest<bool>;

/// <summary>
/// 解散群组命令处理程序。
/// </summary>
public class DismissGroupCommandHandler(
    IGroupRepository groupRepository,
    ILogger<DismissGroupCommandHandler> logger) : IRequestHandler<DismissGroupCommand, bool>
{
    public async Task<bool> Handler(DismissGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.Dismiss();
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 已解散", command.GroupId);
        return true;
    }
}
