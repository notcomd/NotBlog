namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 静音/取消静音好友命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Mute">true 静音，false 取消静音</param>
public record MuteFriendCommand(Guid UserId, Guid FriendId, bool Mute) : IRequest<bool>;

