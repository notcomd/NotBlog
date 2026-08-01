namespace Message.Web.API.Application.Commands.Friends;

/// <summary>
/// 更新好友备注命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Remark">新的备注名称</param>
public record UpdateFriendRemarkCommand(Guid UserId, Guid FriendId, string Remark) : IRequest<bool>;

/// <summary>
/// 更新好友备注命令处理程序。
/// </summary>
public class UpdateFriendRemarkCommandHandler(
    IMessageFriendsRepository friendRepository,
    ILogger<UpdateFriendRemarkCommandHandler> logger) : IRequestHandler<UpdateFriendRemarkCommand, bool>
{
    public async Task<bool> Handler(UpdateFriendRemarkCommand command, CancellationToken cancellationToken)
    {
        var friendship = await friendRepository.GetByUserAndFriendAsync(command.UserId, command.FriendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.UpdateRemark(command.Remark);
        await friendRepository.UpdateAsync(friendship);
        await friendRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 已将好友 {FriendId} 的备注更新为 {Remark}",
            command.UserId, command.FriendId, command.Remark);
        return true;
    }
}
