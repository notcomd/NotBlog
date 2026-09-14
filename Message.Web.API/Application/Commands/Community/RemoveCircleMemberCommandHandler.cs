namespace Message.Web.API.Application.Commands.Community;
/// <summary>移出成员命令处理程序。</summary>
public class RemoveCircleMemberCommandHandler(
    ICircleRepository circleRepository,
    ILogger<RemoveCircleMemberCommandHandler> logger) : IRequestHandler<RemoveCircleMemberCommand, bool>
{
    public async Task<bool> Handler(RemoveCircleMemberCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdWithMembersAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            if (command.UserGuid == command.OperatorGuid)
            {
                // 成员主动退出（圈主不可退出，由领域规则拦截）
                circle.RemoveMember(command.UserGuid);
            }
            else
            {
                var operatorMember = circle.GetActiveMember(command.OperatorGuid);
                if (operatorMember is null || operatorMember.Role == CircleMemberRole.Member)
                    throw new UnauthorizedAccessException("只有圈主或管理员可以移出成员");

                circle.BanMember(command.UserGuid, command.OperatorGuid);
            }

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("成员已移出圈子: Circle={CircleGuid}, User={UserGuid}",
                command.CircleGuid, command.UserGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException)
        {
            logger.LogError(ex, "移出成员失败: Circle={CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
