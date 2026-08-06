namespace Message.Web.API.Application.Commands.Groups;
/// <summary>
/// 创建群组命令处理程序。
/// <para>权限（修复 S-04）：群主（创建者）必须是当前登录用户，禁止伪造他人为群主。</para>
/// </summary>
public class CreateGroupCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<CreateGroupCommandHandler> logger) : IRequestHandler<CreateGroupCommand, Guid>
{
    public async Task<Guid> Handler(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var operatorId = currentUser.GetUserId();
        if (command.UserId != operatorId)
            throw new UnauthorizedAccessException("不能以他人身份创建群组");

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
