namespace Message.Web.API.Application.Queries.UserInfo;

/// <summary>获取用户资料查询。</summary>
public record GetMyUserInfoQuery(Guid UserId) : IRequest<UserInfoDto>;
