
namespace Message.Web.API.APIs;

/// <summary>
/// 话题接口（静态函数模式 + CQRS）。
/// <para>话题为全局轻量标签：任何用户可创建，帖子可关联多个话题（最多 10 个）。</para>
/// </summary>
public static class TopicsApi
{
    /// <summary>映射话题相关端点组</summary>
    public static RouteGroupBuilder MapTopicsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics")
            .WithTags("Topics")
            .RequireAuthorization()
            .RequireResourcePermissions("api:topic");

        group.MapPost("/", CreateTopicAsync)
            .WithSummary("创建话题")
            .Accepts<CreateTopicRequest>("application/json")
            .Produces<ApiResponseResult<Guid>>();

        group.MapGet("/", GetTopicsAsync)
            .WithSummary("话题列表（按帖子数降序）")
            .Produces<ApiResponseResult<PagedResult<TopicDto>>>();

        group.MapGet("/{topicGuid}/posts", GetTopicPostsAsync)
            .WithSummary("话题帖子流")
            .Produces<ApiResponseResult<PagedResult<CommunityPostDto>>>();

        // PUT /{topicGuid} — 更新话题（创建者/管理员；R-12）
        group.MapPut("/{topicGuid}", UpdateTopicAsync)
            .WithSummary("更新话题")
            .WithDescription("更新话题名称与简介，仅创建者本人或管理员可操作")
            .Accepts<UpdateTopicRequest>("application/json")
            .Produces<ApiResponseResult>();

        // DELETE /{topicGuid} — 停用话题（创建者/管理员；R-12）
        group.MapDelete("/{topicGuid}", DeactivateTopicAsync)
            .WithSummary("停用话题")
            .WithDescription("停用后话题不再出现在列表与帖子流中，仅创建者本人或管理员可操作")
            .Produces<ApiResponseResult>();

        return group;
    }

    private static async Task<IResult> CreateTopicAsync(
        [FromBody] CreateTopicRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var topicId = await mediator.SendAsync(new CreateTopicCommand(
                currentUser.GetUserId(), request.Name, request.Description), ct);
            return Results.Ok(ApiResponseResult<Guid>.Created(topicId, "话题创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult<Guid>.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<Guid>.Error($"创建话题失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetTopicsAsync(
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetTopicsQuery(page, pageSize), ct);
            var dto = new PagedResult<TopicDto>
            {
                Items = paged.Items.Select(t => t.ToDto()).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponseResult<PagedResult<TopicDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<PagedResult<TopicDto>>.Error($"获取话题列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetTopicPostsAsync(
        Guid topicGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetTopicPostsQuery(topicGuid, currentUser.GetUserId(), page, pageSize), ct);
            var dto = new PagedResult<CommunityPostDto>
            {
                Items = paged.Items.Select(t => t.ToCommunityDto()).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
            return Results.Ok(ApiResponseResult<PagedResult<CommunityPostDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<PagedResult<CommunityPostDto>>.Error($"获取话题帖子失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateTopicAsync(
        Guid topicGuid,
        [FromBody] UpdateTopicRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UpdateTopicCommand(topicGuid, request.Name, request.Description), ct);
            return Results.Ok(ApiResponseResult.Ok("话题更新成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponseResult.NotFound(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(ApiResponseResult.Forbidden(ex.Message), statusCode: 403);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"更新话题失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> DeactivateTopicAsync(
        Guid topicGuid,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DeactivateTopicCommand(topicGuid), ct);
            return Results.Ok(ApiResponseResult.Ok("话题已停用"));
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
            return Results.Json(ApiResponseResult.Error($"停用话题失败: {ex.Message}"), statusCode: 500);
        }
    }
}

