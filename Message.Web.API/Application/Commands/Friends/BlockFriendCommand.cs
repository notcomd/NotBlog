namespace Message.Web.API.Application.Commands.Friends;

/// <summary>
/// 屏蔽/取消屏蔽好友命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Block">true 屏蔽，false 取消屏蔽</param>
public record BlockFriendCommand(Guid UserId, Guid FriendId, bool Block) : IRequest<bool>;

/// <summary>
/// 屏蔽/取消屏蔽好友命令处理程序。
/// </summary>
public class BlockFriendCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<BlockFriendCommandHandler> logger) : IRequestHandler<BlockFriendCommand, bool>
{
    public async Task<bool> Handler(BlockFriendCommand command, CancellationToken cancellationToken)
    {
        if (command.Block)
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Block();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Unblock();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("用户 {UserId} 已{Action}好友 {FriendId}",
            command.UserId, command.Block ? "屏蔽" : "取消屏蔽", command.FriendId);
        return true;
    }
}
