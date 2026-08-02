namespace Message.Web.API.Application.Commands.Groups;

/// <summary>
/// 禁言群组成员命令。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">被禁言的用户 ID</param>
/// <param name="DurationMinutes">禁言时长（分钟）</param>
public record MuteGroupMemberCommand(Guid GroupId, Guid UserId, int DurationMinutes) : IRequest<bool>;

/// <summary>
/// 禁言群组成员命令处理程序。
/// <para>权限（修复 S-04）：需群主/管理员。</para>
/// </summary>
public class MuteGroupMemberCommandHandler(
    IGroupRepository groupRepository,
    ICurrentUserService currentUser,
    ILogger<MuteGroupMemberCommandHandler> logger) : IRequestHandler<MuteGroupMemberCommand, bool>
{
    public async Task<bool> Handler(MuteGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(command.GroupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var operatorId = currentUser.GetUserId();
        if (!group.HasPermission(operatorId, GroupPermission.MuteMember))
            throw new UnauthorizedAccessException("仅群主或管理员可禁言成员");

        group.MuteMember(command.UserId, TimeSpan.FromMinutes(command.DurationMinutes));
        await groupRepository.UpdateAsync(group);
        await groupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("群组 {GroupId} 中用户 {UserId} 已被禁言 {DurationMinutes} 分钟",
            command.GroupId, command.UserId, command.DurationMinutes);
        return true;
    }
}
