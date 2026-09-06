
namespace Message.Web.API.APIs;

/// <summary>
/// 评论接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 路由参数（如 {tweetGuid}、{commentGuid}）由框架按名称绑定，请求体使用 <c>[FromBody]</c>；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class CommentsApi
{
    /// <summary>映射评论相关端点组</summary>
    public static RouteGroupBuilder MapCommentsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comments")
            .WithTags("Comments")
            .RequireAuthorization()
            .RequireResourcePermissions("api:comment");

        
        group.MapPost("/", AddCommentAsync)
            .WithSummary("发布评论")
            .WithDescription("对推文发布评论或回复")
            .Accepts<CreateCommentRequest>("application/json")
            .Produces<ApiResponse>();

       
        group.MapGet("/tweet/{tweetGuid}", GetTweetCommentsAsync)
            .WithSummary("获取推文评论")
            .WithDescription("获取指定推文的评论列表，支持分页")
            .Produces<ApiResponse<PagedResult<CommentDto>>>();

      
        group.MapGet("/{commentGuid}/replies", GetCommentRepliesAsync)
            .WithSummary("获取评论回复")
            .WithDescription("获取指定评论的回复列表，支持分页")
            .Produces<ApiResponse<PagedResult<CommentDto>>>();

        
        group.MapDelete("/{commentGuid}", DeleteCommentAsync)
            .WithSummary("删除评论")
            .WithDescription("删除指定评论")
            .Produces<ApiResponse>();

        return group;
    }

    /// <summary>
    /// 发布评论或回复（命令侧）。
    /// </summary>
    /// <param name="request">创建评论请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> AddCommentAsync(
        [FromBody] CreateCommentRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new AddCommentCommand(
                request.TweetGuid, userId, request.Content, request.ParentGuid, request.ReplyToGuid), ct);

            return Results.Ok(ApiResponse.Ok("评论发布成功"));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"发布评论失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取推文的评论列表（查询侧，分页）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页评论列表</returns>
    private static async Task<IResult> GetTweetCommentsAsync(
        Guid tweetGuid,
        [FromServices] INotMediator mediator,
        [FromServices] ICommentRepository commentRepository,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var comments = await mediator.SendAsync(new GetTweetCommentsQuery(tweetGuid, page, pageSize), ct);

            var result = new PagedResult<CommentDto>
            {
                Items = [.. comments.Select(MapToDto)],
                // F-06：TotalCount 为总记录数（独立 CountAsync，而非当前页数量）
                TotalCount = await commentRepository.GetCountByTweetAsync(tweetGuid),
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommentDto>>.Error($"获取推文评论失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取指定评论的回复列表（查询侧，分页）。
    /// </summary>
    /// <param name="commentGuid">评论ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页回复列表</returns>
    private static async Task<IResult> GetCommentRepliesAsync(
        Guid commentGuid,
        [FromServices] INotMediator mediator,
        [FromServices] ICommentRepository commentRepository,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var replies = await mediator.SendAsync(new GetCommentRepliesQuery(commentGuid, page, pageSize), ct);

            var result = new PagedResult<CommentDto>
            {
                Items = [.. replies.Select(MapToDto)],
                // F-06：TotalCount 为总记录数（独立 CountAsync，而非当前页数量）
                TotalCount = await commentRepository.GetReplyCountAsync(commentGuid),
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<CommentDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommentDto>>.Error($"获取评论回复失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 删除评论（命令侧，仅限作者本人）。
    /// </summary>
    /// <param name="commentGuid">评论ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DeleteCommentAsync(
        Guid commentGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new DeleteCommentCommand(commentGuid, userId), ct);

            return Results.Ok(ApiResponse.Ok("评论已删除"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除评论失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>评论实体 → DTO 映射</summary>
    private static CommentDto MapToDto(Comment c) => new()
    {
        CommentGuid = c.CommentGuid,
        TweetGuid = c.TweetGuid,
        User = new UserBriefDto { UserGuid = c.UserGuid, UserName = string.Empty },
        ParentGuid = c.ParentGuid,
        ReplyToGuid = c.ReplyToGuid,
        Content = c.Content,
        LikeCount = c.LikeCount,
        ReplyCount = c.ReplyCount,
        IsDeleted = c.IsDeleted,
        CreateTime = c.CreateTime
    };
}
