
namespace Message.Web.API.Application.Commands.Community;
/// <summary>
/// 解散圈子命令处理程序。
/// <para>联动：解散圈子的同时解散社区群组（按 Group.CircleId 反查，无群组的存量社区跳过）。
/// 群解散事件由既有 GroupDissolvedEventHandler 负责通知群成员并同步解散群聊会话；
/// 社区侧会话同步解散见 CircleDissolvedEventHandler——两者均在本次 SaveEntitiesAsync 的同一批领域事件内派发。</para>
/// </summary>
public class DissolveCircleCommandHandler(
    ICircleRepository circleRepository,
    IGroupRepository groupRepository,
    ILogger<DissolveCircleCommandHandler> logger) : IRequestHandler<DissolveCircleCommand, bool>
{
    public async Task<bool> Handler(DissolveCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            if (circle.OwnerGuid != command.OperatorGuid)
                throw new UnauthorizedAccessException("只有圈主可以解散圈子");

            circle.Dissolve();

            // 联动：同步解散社区群组
            var group = await groupRepository.GetByCircleIdWithMembersAsync(command.CircleGuid);
            if (group is not null && !group.IsDismissed)
            {
                group.Dismiss();
                await groupRepository.UpdateAsync(group);
            }

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子已解散: {CircleGuid}", command.CircleGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "解散圈子失败: {CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
