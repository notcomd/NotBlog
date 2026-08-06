namespace Message.Web.API.Application.Commands.Community;
/// <summary>凭邀请码/链接加入圈子命令处理程序。</summary>
public class JoinCircleCommandHandler(
    ICircleRepository circleRepository,
    ICircleInvitationRepository invitationRepository,
    ILogger<JoinCircleCommandHandler> logger) : IRequestHandler<JoinCircleCommand, Guid>
{
    public async Task<Guid> Handler(JoinCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command.Code) && command.Token is null)
                throw new ArgumentException("邀请码或邀请链接必须提供其一");

            var invitation = string.IsNullOrWhiteSpace(command.Code)
                ? await invitationRepository.GetByTokenAsync(command.Token!.Value)
                : await invitationRepository.GetByCodeAsync(command.Code.Trim().ToUpperInvariant());
            if (invitation is null)
                throw new KeyNotFoundException("邀请不存在或已失效");

            if (!invitation.IsValid())
                throw new InvalidOperationException("邀请已失效（已使用、已撤销或已过期）");

            var circle = await circleRepository.GetByIdWithMembersAsync(invitation.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在或已解散");

            circle.AddMember(command.UserId, CircleMemberRole.Member, null, invitation.InviterGuid);
            invitation.Accept();

            await circleRepository.UpdateAsync(circle);
            await invitationRepository.UpdateAsync(invitation);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("用户加入圈子: User={UserGuid}, Circle={CircleGuid}, Invite={InviteGuid}",
                command.UserId, circle.CircleGuid, invitation.InviteGuid);
            return circle.CircleGuid;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException and not ArgumentException)
        {
            logger.LogError(ex, "加入圈子失败，用户: {UserGuid}", command.UserId);
            throw;
        }
    }
}
