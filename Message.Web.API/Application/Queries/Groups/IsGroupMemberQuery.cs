namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 检查用户是否为群组成员查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">用户 ID</param>
public record IsGroupMemberQuery(Guid GroupId, Guid UserId) : IRequest<bool>;

