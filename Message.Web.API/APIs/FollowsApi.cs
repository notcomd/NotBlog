
namespace Message.Web.API.APIs;

/// <summary>
/// 关注/粉丝接口（静态函数模式 + CQRS）。
/// <para>全局关注关系（微博式）：关注人 → 聚合关注 Feed；粉丝列表按时间倒序。</para>
/// </summary>
public static class FollowsApi
{
    /// <summary>映射关注相关端点组</summary>
    public static RouteGroupBuilder MapFollowsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/follows")
            .WithTags("Follows")
            .RequireAuthorization()
            .RequireResourcePermissions("api:follow");

        group.MapPost("/{userGuid}", FollowUserAsync)
            .WithSummary("关注用户")
            .Produces<ApiResponse>();

        group.MapDelete("/{userGuid}", UnfollowUserAsync)
            .WithSummary("取消关注")
            .Produces<ApiResponse>();

        group.MapGet("/following", GetFollowingAsync)
            .WithSummary("我关注的人")
            .Produces<ApiResponse<PagedResult<UserFollowDto>>>();

        group.MapGet("/followers", GetFollowersAsync)
            .WithSummary("我的粉丝")
            .Produces<ApiResponse<PagedResult<UserFollowDto>>>();

        group.MapGet("/feed", GetFeedAsync)
            .WithSummary("关注 Feed（我 + 关注者的全局帖）")
            .Produces<ApiResponse<PagedResult<CommunityPostDto>>>();

        return group;
    }

    private static async Task<IResult> FollowUserAsync(
        Guid userGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new FollowUserCommand(currentUser.GetUserId(), userGuid), ct);
            return Results.Ok(ApiResponse.Ok("关注成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Ok(ApiResponse.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"关注失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> UnfollowUserAsync(
        Guid userGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UnfollowUserCommand(currentUser.GetUserId(), userGuid), ct);
            return Results.Ok(ApiResponse.Ok("已取消关注"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"取消关注失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetFollowingAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetFollowingQuery(currentUser.GetUserId(), page, pageSize), ct);
            return Results.Ok(ApiResponse<PagedResult<UserFollowDto>>.Ok(MapFollows(paged, f => f.FolloweeGuid)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<UserFollowDto>>.Error($"获取关注列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetFollowersAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetFollowersQuery(currentUser.GetUserId(), page, pageSize), ct);
            return Results.Ok(ApiResponse<PagedResult<UserFollowDto>>.Ok(MapFollows(paged, f => f.FollowerGuid)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<UserFollowDto>>.Error($"获取粉丝列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetFeedAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetCommunityFeedQuery(currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CommunityPostDto>
            {
                Items = paged.Items.Select(t => t.ToCommunityDto()).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponse<PagedResult<CommunityPostDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommunityPostDto>>.Error($"获取关注 Feed 失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static PagedResult<UserFollowDto> MapFollows(
        PagedResult<Message.Domain.Entities.Community.UserFollow> paged,
        Func<Message.Domain.Entities.Community.UserFollow, Guid> userGuidSelector) => new()
    {
        Items = paged.Items.Select(f => new UserFollowDto
        {
            UserGuid = userGuidSelector(f),
            CreateTime = f.CreateTime
        }).ToList(),
        TotalCount = paged.TotalCount,
        Page = paged.Page,
        PageSize = paged.PageSize
    };
}
