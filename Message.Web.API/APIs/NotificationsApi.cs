namespace Message.Web.API.APIs;

/// <summary>
/// 站内通知接口（静态函数模式 + CQRS，R-04）。
/// <para>通知由领域事件处理器写入（审核/评论/关注/圈子邀请/举报处理），本组端点提供读侧与已读管理。</para>
/// </summary>
public static class NotificationsApi
{
    /// <summary>映射通知端点组</summary>
    public static RouteGroupBuilder MapNotificationsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireResourcePermissions("api:notification");

        // GET / — 我的通知列表（分页，可按未读过滤）
        group.MapGet("/", GetNotificationsAsync)
            .WithSummary("我的通知列表")
            .WithDescription("获取当前用户的通知列表，支持分页与未读过滤")
            .Produces<ApiResponseResult<PagedResult<NotificationDto>>>();

        // GET /unread-count — 未读数
        group.MapGet("/unread-count", GetUnreadCountAsync)
            .WithSummary("未读通知数")
            .WithDescription("获取当前用户的未读通知数量")
            .Produces<ApiResponseResult<int>>();

        // PUT /read-all — 全部已读
        group.MapPut("/read-all", MarkAllReadAsync)
            .WithSummary("全部标记已读")
            .Produces<ApiResponseResult>();

        // PUT /{notifyGuid}/read — 单条已读
        group.MapPut("/{notifyGuid}/read", MarkReadAsync)
            .WithSummary("标记单条通知已读")
            .Produces<ApiResponseResult>();

        return group;
    }

    private static async Task<IResult> GetNotificationsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetMyNotificationsQuery(
                currentUser.GetUserId(), page, pageSize, unreadOnly), ct);

            var result = new PagedResult<NotificationDto>
            {
                Items = [.. paged.Items.Select(n => n.ToDto())],
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponseResult<PagedResult<NotificationDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<PagedResult<NotificationDto>>.Error($"获取通知列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetUnreadCountAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct = default)
    {
        try
        {
            var count = await mediator.SendAsync(new GetUnreadNotificationCountQuery(currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponseResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<int>.Error($"获取未读数失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> MarkAllReadAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct = default)
    {
        try
        {
            await mediator.SendAsync(new MarkAllNotificationsReadCommand(currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponseResult.Ok("全部通知已标记为已读"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"标记全部已读失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notifyGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct = default)
    {
        try
        {
            await mediator.SendAsync(new MarkNotificationReadCommand(notifyGuid, currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponseResult.Ok("通知已标记为已读"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponseResult.NotFound(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(ApiResponseResult.Forbidden(ex.Message), statusCode: 403);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"标记已读失败: {ex.Message}"), statusCode: 500);
        }
    }
}
