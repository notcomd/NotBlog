namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 发送好友请求命令处理程序。
/// </summary>
public class SendFriendRequestCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<SendFriendRequestCommandHandler> logger) : IRequestHandler<SendFriendRequestCommand, Guid>
{
    public async Task<Guid> Handler(SendFriendRequestCommand command, CancellationToken cancellationToken)
    {
        var friendship = new MessageFriends(command.UserId, command.FriendId);
        await friendRepository.AddAsync(friendship);
        await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 向 {FriendId} 发送了好友请求，友谊关系 {FriendshipId}",
            command.UserId, command.FriendId, friendship.FriendshipId);
        return friendship.FriendshipId;
    }
}
