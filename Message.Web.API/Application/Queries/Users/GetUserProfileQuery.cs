namespace Message.Web.API.Application.Queries.Users;

/// <summary>获取用户公开信息查询（个人主页聚合：资料 + 关注/粉丝计数 + 作品数 + 获赞总数 + 是否已关注）。</summary>
public record GetUserProfileQuery(Guid UserGuid, Guid ViewerId) : IRequest<UserProfileDto>;