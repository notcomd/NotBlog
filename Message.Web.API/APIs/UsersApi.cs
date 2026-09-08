namespace Message.Web.API.APIs;

/// <summary>
/// 用户公开信息接口（静态函数模式 + CQRS）。
/// <para>个人主页聚合端点：资料（昵称/头像/bio）+ 关注/粉丝计数 + 作品数 + 获赞总数 + 是否已关注。</para>
/// </summary>
public static class UsersApi
{
    /// <summary>映射用户公开信息端点组</summary>
    public static RouteGroupBuilder MapUsersApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // GET /{userGuid} — 用户公开信息（个人主页）
        group.MapGet("/{userGuid}", GetUserProfileAsync)
            .RequirePermission("api:userinfo:read")
            .WithSummary("用户公开信息")
            .WithDescription("获取指定用户的资料、关注/粉丝计数、作品数与获赞总数，以及当前登录用户是否已关注对方")
            .Produces<ApiResponseResult<UserProfileDto>>();

        return group;
    }

    private static async Task<IResult> GetUserProfileAsync(
        Guid userGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.IsAuthenticated ? currentUser.GetUserId() : Guid.Empty;
            var dto = await mediator.SendAsync(new GetUserProfileQuery(userGuid, userId), ct);
            return Results.Ok(ApiResponseResult<UserProfileDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<UserProfileDto>.Error($"获取用户信息失败: {ex.Message}"), statusCode: 500);
        }
    }
}