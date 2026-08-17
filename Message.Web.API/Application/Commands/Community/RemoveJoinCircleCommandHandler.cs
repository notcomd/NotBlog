namespace Message.Web.API.Application.Commands.Community;


public class RemoveJoinCircleCommandHandler(
    ICircleRepository circleRepository,
    ICircleInvitationRepository memberRepository,
    ILogger<RemoveJoinCircleCommandHandler> logger) : IRequestHandler<RemoveJoinCircleCommand, bool>
{
    public async Task<bool> Handler(RemoveJoinCircleCommand command, CancellationToken cancellationToken)
    {
        _ = await circleRepository.GetByIdWithMembersAsync(command.CircleGuid)
            ?? throw new KeyNotFoundException("圈子不存在或已解散");
        
        var join= await memberRepository.GetByIdAsync(command.CircleGuid)?? throw new KeyNotFoundException("圈子成员不存在或已退出");

        await memberRepository.DeleteAsync(join.InviteGuid);

        await memberRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("邀请码已被使用，并进行删除: User={UserGuid}, Circle={CircleGuid}",
            command.UserGuid, command.CircleGuid);
        return true;
    }
}