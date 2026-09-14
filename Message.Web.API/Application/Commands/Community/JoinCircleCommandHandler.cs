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

            // 邀请一次性使用：先原子占用（仅 Pending 且未过期可占用成功），并发下第二个请求在此失败
            if (!await invitationRepository.TryAcceptAtomicallyAsync(invitation.InviteGuid))
                throw new InvalidOperationException("邀请已失效（已使用、已撤销或已过期）");

            var circle = await circleRepository.GetByIdWithMembersAsync(invitation.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在或已解散");

            circle.AddMember(command.UserId, CircleMemberRole.Member, null, invitation.InviterGuid);
            invitation.Accept();

            await circleRepository.UpdateAsync(circle);
            // 邀请（码/链接）一次性使用：用后物理删除，避免已用邀请残留在邀请列表中
            await invitationRepository.DeleteAsync(invitation.InviteGuid);
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
