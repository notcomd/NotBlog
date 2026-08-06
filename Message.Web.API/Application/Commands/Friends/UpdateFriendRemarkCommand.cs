namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 更新好友备注命令。
/// </summary>
/// <param name="UserId">当前用户 ID</param>
/// <param name="FriendId">好友用户 ID</param>
/// <param name="Remark">新的备注名称</param>
public record UpdateFriendRemarkCommand(Guid UserId, Guid FriendId, string Remark) : IRequest<bool>;

