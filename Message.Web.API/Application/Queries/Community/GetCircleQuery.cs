namespace Message.Web.API.Application.Queries.Community;

/// <summary>
/// 圈子详情查询（基本信息公开可看；MyRole/IsMember 区分当前用户身份）。
/// </summary>
public record GetCircleQuery(Guid CircleGuid, Guid CurrentUserId) : IRequest<CircleDetailResult>;

