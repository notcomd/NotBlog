namespace Message.Web.API.Application.Commands.Friends;
/// <summary>
/// 发送好友请求命令。
/// <para>CQRS 命令侧：仅返回新好友关系的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">发起请求的用户 ID</param>
/// <param name="FriendId">接收请求的用户 ID</param>
public record SendFriendRequestCommand(Guid UserId, Guid FriendId) : IRequest<Guid>;

