namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 创建群组命令。
/// <para>CQRS 命令侧：仅返回新群组的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">群主（创建者）用户 ID</param>
/// <param name="GroupName">群名称</param>
/// <param name="MaxMembers">最大成员数</param>
/// <param name="IsPublic">是否公开群组</param>
/// <param name="InitialMembers">初始成员集合（可选）</param>
public record CreateGroupCommand(
    Guid UserId,
    string GroupName,
    int MaxMembers,
    bool IsPublic,
    HashSet<Guid>? InitialMembers) : IRequest<Guid>;

/// <summary>
/// 创建群组命令处理程序。
/// </summary>
public class CreateGroupCommandHandler(
    IGroupRepository groupRepository,
    ILogger<CreateGroupCommandHandler> logger) : IRequestHandler<CreateGroupCommand, Guid>
{
    public async Task<Guid> Handler(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = new Group(command.UserId, command.GroupName, command.MaxMembers, command.IsPublic);
        await groupRepository.AddAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (command.InitialMembers != null && command.InitialMembers.Any())
        {
            foreach (var memberId in command.InitialMembers)
            {
                var groupWithMembers = await groupRepository.GetByIdWithMembersAsync(group.GroupId);
                if (groupWithMembers == null)
                    throw new KeyNotFoundException("群组不存在");

                groupWithMembers.AddMember(memberId);
                await groupRepository.UpdateAsync(groupWithMembers);
                await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            }
        }

        logger.LogInformation("用户 {UserId} 创建了群组 {GroupId}（{GroupName}），初始成员数 {MemberCount}",
            command.UserId, group.GroupId, group.GroupName, command.InitialMembers?.Count ?? 0);
        return group.GroupId;
    }
}
