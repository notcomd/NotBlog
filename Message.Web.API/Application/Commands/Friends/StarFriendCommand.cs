namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 星标/取消星标好友命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Star">true 星标，false 取消星标</param>
public record StarFriendCommand(Guid UserId, Guid FriendId, bool Star) : IRequest<bool>;

