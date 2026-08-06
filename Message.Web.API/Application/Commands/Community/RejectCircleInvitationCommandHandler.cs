namespace Message.Web.API.Application.Commands.Community;
/// <summary>拒绝直邀命令处理程序。</summary>
public class RejectCircleInvitationCommandHandler(
    ICircleInvitationRepository invitationRepository,
    ILogger<RejectCircleInvitationCommandHandler> logger) : IRequestHandler<RejectCircleInvitationCommand, bool>
{
    public async Task<bool> Handler(RejectCircleInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await invitationRepository.GetByIdAsync(command.InviteGuid)
                ?? throw new KeyNotFoundException("邀请不存在或已失效");

            if (invitation.Type != CircleInvitationType.Direct)
                throw new InvalidOperationException("该邀请不是直邀");
            if (invitation.InviteeGuid != command.UserId)
                throw new UnauthorizedAccessException("该邀请不是发给你的");

            invitation.Revoke(command.UserId);

            await invitationRepository.UpdateAsync(invitation);
            await invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("用户拒绝直邀: User={UserGuid}, Invite={InviteGuid}", command.UserId, command.InviteGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException)
        {
            logger.LogError(ex, "拒绝直邀失败: Invite={InviteGuid}", command.InviteGuid);
            throw;
        }
    }
}
