namespace Message.Web.API.Application.Commands.Community;
/// <summary>取消关注命令处理程序。</summary>
public class UnfollowUserCommandHandler(
    IUserFollowRepository followRepository,
    ILogger<UnfollowUserCommandHandler> logger) : IRequestHandler<UnfollowUserCommand, bool>
{
    public async Task<bool> Handler(UnfollowUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!await followRepository.ExistsAsync(command.FollowerGuid, command.FolloweeGuid))
                throw new KeyNotFoundException("尚未关注该用户");

            await followRepository.DeleteAsync(command.FollowerGuid, command.FolloweeGuid);
            await followRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("取消关注成功: Follower={FollowerGuid}, Followee={FolloweeGuid}",
                command.FollowerGuid, command.FolloweeGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "取消关注失败: Followee={FolloweeGuid}", command.FolloweeGuid);
            throw;
        }
    }
}
