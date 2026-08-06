namespace Message.Web.API.Application.Commands.Community;
/// <summary>设置/取消管理员命令处理程序。</summary>
public class SetCircleMemberRoleCommandHandler(
    ICircleRepository circleRepository,
    ILogger<SetCircleMemberRoleCommandHandler> logger) : IRequestHandler<SetCircleMemberRoleCommand, bool>
{
    public async Task<bool> Handler(SetCircleMemberRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdWithMembersAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            var role = command.Role.ToLowerInvariant() switch
            {
                "admin" => CircleMemberRole.Admin,
                "member" => CircleMemberRole.Member,
                _ => throw new ArgumentException("角色必须是 admin 或 member", nameof(command.Role))
            };

            circle.SetMemberRole(command.UserGuid, role, command.OperatorGuid);

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("成员角色已变更: Circle={CircleGuid}, User={UserGuid}, Role={Role}",
                command.CircleGuid, command.UserGuid, role);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException and not ArgumentException)
        {
            logger.LogError(ex, "设置成员角色失败: Circle={CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
