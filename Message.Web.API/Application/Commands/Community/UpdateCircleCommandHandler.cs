namespace Message.Web.API.Application.Commands.Community;
/// <summary>更新圈子信息命令处理程序。</summary>
public class UpdateCircleCommandHandler(
    ICircleRepository circleRepository,
    ILogger<UpdateCircleCommandHandler> logger) : IRequestHandler<UpdateCircleCommand, bool>
{
    public async Task<bool> Handler(UpdateCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdWithMembersAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            var operatorMember = circle.GetActiveMember(command.OperatorGuid);
            if (operatorMember is null || operatorMember.Role == CircleMemberRole.Member)
                throw new UnauthorizedAccessException("只有圈主或管理员可以更新圈子信息");

            circle.UpdateInfo(command.Name, command.Description, command.AvatarUrl, command.CoverUrl);

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子信息已更新: {CircleGuid}", command.CircleGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "更新圈子信息失败: {CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
