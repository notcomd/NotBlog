namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 删除好友关系命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
public record DeleteFriendCommand(Guid UserId, Guid FriendId) : IRequest<bool>;

