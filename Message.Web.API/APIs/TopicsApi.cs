
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
            .RequireAuthorization();

        group.MapPost("/", CreateTopicAsync)
            .WithSummary("创建话题")
            .Accepts<CreateTopicRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        group.MapGet("/", GetTopicsAsync)
            .WithSummary("话题列表（按帖子数降序）")
            .Produces<ApiResponse<PagedResult<TopicDto>>>();

        group.MapGet("/{topicGuid}/posts", GetTopicPostsAsync)
            .WithSummary("话题帖子流")
            .Produces<ApiResponse<PagedResult<CommunityPostDto>>>();

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
            return Results.Ok(ApiResponse<Guid>.Created(topicId, "话题创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Ok(ApiResponse<Guid>.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"创建话题失败: {ex.Message}"), statusCode: 500);
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
            return Results.Ok(ApiResponse<PagedResult<TopicDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<TopicDto>>.Error($"获取话题列表失败: {ex.Message}"), statusCode: 500);
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
            return Results.Ok(ApiResponse<PagedResult<CommunityPostDto>>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommunityPostDto>>.Error($"获取话题帖子失败: {ex.Message}"), statusCode: 500);
        }
    }
}
