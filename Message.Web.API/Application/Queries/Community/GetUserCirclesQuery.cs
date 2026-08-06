namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 我加入的圈子列表查询。
/// </summary>
public record GetUserCirclesQuery(Guid UserId) : IRequest<IEnumerable<Circle>>;

