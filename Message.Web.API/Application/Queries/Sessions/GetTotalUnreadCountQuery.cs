
namespace Message.Web.API.Application.Queries.Sessions;
/// <summary>
/// 获取用户所有会话未读消息总数查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetTotalUnreadCountQuery(Guid UserId) : IRequest<int>;

