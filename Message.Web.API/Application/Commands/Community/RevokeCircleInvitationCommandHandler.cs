
namespace Message.Web.API.Application.Commands.Community;
/// <summary>撤销圈子邀请命令处理程序。</summary>
public class RevokeCircleInvitationCommandHandler(
    ICircleInvitationRepository invitationRepository,
    ICircleRepository circleRepository,
    ILogger<RevokeCircleInvitationCommandHandler> logger) : IRequestHandler<RevokeCircleInvitationCommand, bool>
{
    public async Task<bool> Handler(RevokeCircleInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await invitationRepository.GetByIdAsync(command.InviteGuid)
                ?? throw new KeyNotFoundException("邀请不存在");

            // 权限：圈主/管理员可撤销；直邀的被邀请人可拒绝（复用撤销）
            var isManager = false;
            var member = await circleRepository.GetMemberAsync(invitation.CircleGuid, command.OperatorGuid);
            if (member is not null && member.Role != CircleMemberRole.Member)
                isManager = true;
            var isInvitee = invitation.InviteeGuid == command.OperatorGuid;

            if (!isManager && !isInvitee)
                throw new UnauthorizedAccessException("无权撤销该邀请");

            invitation.Revoke(command.OperatorGuid);

            await invitationRepository.UpdateAsync(invitation);
            await invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("邀请已撤销: Invite={InviteGuid}, Operator={OperatorGuid}", command.InviteGuid, command.OperatorGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException)
        {
            logger.LogError(ex, "撤销邀请失败: Invite={InviteGuid}", command.InviteGuid);
            throw;
        }
    }
}
