
namespace Message.Web.API.Application.Commands.Community;
/// <summary>关注用户命令处理程序。</summary>
public class FollowUserCommandHandler(
    IUserFollowRepository followRepository,
    ILogger<FollowUserCommandHandler> logger) : IRequestHandler<FollowUserCommand, bool>
{
    public async Task<bool> Handler(FollowUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (await followRepository.ExistsAsync(command.FollowerGuid, command.FolloweeGuid))
                throw new InvalidOperationException("已经关注该用户");

            var follow = UserFollow.Create(command.FollowerGuid, command.FolloweeGuid);

            await followRepository.AddAsync(follow);
            await followRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("关注成功: Follower={FollowerGuid}, Followee={FolloweeGuid}",
                command.FollowerGuid, command.FolloweeGuid);
            return true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            logger.LogError(ex, "关注失败: Followee={FolloweeGuid}", command.FolloweeGuid);
            throw;
        }
    }
}
