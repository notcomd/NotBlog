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
/// </summary>
public class UpdateGroupInfoCommandHandler(
    IGroupRepository groupRepository,
    ILogger<UpdateGroupInfoCommandHandler> logger) : IRequestHandler<UpdateGroupInfoCommand, bool>
{
    public async Task<bool> Handler(UpdateGroupInfoCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdateGroupInfo(command.GroupName, command.Description, null);
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 的信息已更新", command.GroupId);
        return true;
    }
}
