namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 静音/取消静音好友命令处理程序。
/// </summary>
public class MuteFriendCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<MuteFriendCommandHandler> logger) : IRequestHandler<MuteFriendCommand, bool>
{
    public async Task<bool> Handler(MuteFriendCommand command, CancellationToken cancellationToken)
    {
        if (command.Mute)
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Mute();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
            if (friendship == null)
                throw new KeyNotFoundException("好友关系不存在");

            friendship.Unmute();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("用户 {UserId} 已{Action}好友 {FriendId}",
            command.UserId, command.Mute ? "静音" : "取消静音", command.FriendId);
        return true;
    }
}
