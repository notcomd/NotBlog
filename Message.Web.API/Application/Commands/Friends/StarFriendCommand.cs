namespace Message.Web.API.Application.Commands.Friends;

/// <summary>
/// 星标/取消星标好友命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Star">true 星标，false 取消星标</param>
public record StarFriendCommand(Guid UserId, Guid FriendId, bool Star) : IRequest<bool>;

/// <summary>
/// 星标/取消星标好友命令处理程序。
/// </summary>
public class StarFriendCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<StarFriendCommandHandler> logger) : IRequestHandler<StarFriendCommand, bool>
{
    public async Task<bool> Handler(StarFriendCommand command, CancellationToken cancellationToken)
    {
        if (command.Star)
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Star();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Unstar();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("用户 {UserId} 已{Action}好友 {FriendId}",
            command.UserId, command.Star ? "星标" : "取消星标", command.FriendId);
        return true;
    }
}
