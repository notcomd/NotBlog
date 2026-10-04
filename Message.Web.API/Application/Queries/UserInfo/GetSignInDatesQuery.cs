namespace Message.Web.API.Application.Queries.UserInfo;

/// <summary>查询用户在 [From, To] 闭区间内的签到日期（供签到热力图渲染）。</summary>
public record GetSignInDatesQuery(Guid UserId, DateOnly From, DateOnly To) : IRequest<SignInDatesDto>;
