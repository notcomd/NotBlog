namespace Message.Web.API.Application.Commands.Community;
/// <summary>接受直邀命令处理程序。</summary>
public class AcceptCircleInvitationCommandHandler(
    ICircleRepository circleRepository,
    ICircleInvitationRepository invitationRepository,
    ILogger<AcceptCircleInvitationCommandHandler> logger) : IRequestHandler<AcceptCircleInvitationCommand, Guid>
{
    public async Task<Guid> Handler(AcceptCircleInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await invitationRepository.GetByIdAsync(command.InviteGuid)
                ?? throw new KeyNotFoundException("邀请不存在或已失效");

            if (invitation.Type != CircleInvitationType.Direct)
                throw new InvalidOperationException("该邀请不是直邀，请使用邀请码或链接加入");
            if (invitation.InviteeGuid != command.UserId)
                throw new UnauthorizedAccessException("该邀请不是发给你的");
            if (!invitation.IsValid())
                throw new InvalidOperationException("邀请已失效（已使用、已撤销或已过期）");

            var circle = await circleRepository.GetByIdWithMembersAsync(invitation.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在或已解散");

            circle.AddMember(command.UserId, CircleMemberRole.Member, null, invitation.InviterGuid);
            invitation.Accept();

            await circleRepository.UpdateAsync(circle);
            await invitationRepository.UpdateAsync(invitation);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("用户接受直邀加入圈子: User={UserGuid}, Circle={CircleGuid}",
                command.UserId, circle.CircleGuid);
            return circle.CircleGuid;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException)
        {
            logger.LogError(ex, "接受直邀失败，用户: {UserGuid}", command.UserId);
            throw;
        }
    }
}
