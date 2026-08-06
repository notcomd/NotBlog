namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 处理好友请求命令（接受或拒绝）。
/// </summary>
/// <param name="UserId">当前用户 ID（请求接收方）</param>
/// <param name="FriendId">发送请求的用户 ID</param>
/// <param name="Accept">true 接受，false 拒绝</param>
public record HandleFriendRequestCommand(Guid UserId, Guid FriendId, bool Accept) : IRequest<bool>;

