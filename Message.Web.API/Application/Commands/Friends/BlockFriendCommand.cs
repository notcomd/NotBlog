namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 屏蔽/取消屏蔽好友命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Block">true 屏蔽，false 取消屏蔽</param>
public record BlockFriendCommand(Guid UserId, Guid FriendId, bool Block) : IRequest<bool>;

