namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 处理好友请求命令处理程序。
/// </summary>
public class HandleFriendRequestCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<HandleFriendRequestCommandHandler> logger) : IRequestHandler<HandleFriendRequestCommand, bool>
{
    public async Task<bool> Handler(HandleFriendRequestCommand command, CancellationToken cancellationToken)
    {
        if (command.Accept)
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.FriendId, command.UserId);
            if (friendship == null)
                throw new KeyNotFoundException("好友请求不存在");

            friendship.Accept();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else
        {
            var friendship = await friendRepository.GetByUserAndFriendAsync(command.FriendId, command.UserId);
            if (friendship == null)
                throw new KeyNotFoundException("好友请求不存在");

            friendship.Reject();
            await friendRepository.UpdateAsync(friendship);
            await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        logger.LogInformation("用户 {UserId} 已{Action}来自 {FriendId} 的好友请求",
            command.UserId, command.Accept ? "接受" : "拒绝", command.FriendId);
        return true;
    }
}
