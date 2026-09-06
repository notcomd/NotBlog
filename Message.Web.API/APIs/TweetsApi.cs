using Message.Web.API.Application.Commands.Tweets;

namespace Message.Web.API.APIs;

/// <summary>
/// 推文接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果。
/// </para>
/// </summary>
public static class TweetsApi
{
    /// <summary>映射推文相关端点组</summary>
    public static RouteGroupBuilder MapTweetsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tweets")
            .WithTags("Tweets")
            .RequireAuthorization()
            .RequireResourcePermissions("api:tweet");

        // POST / — 创建推文
        group.MapPost("/", CreateTweetAsync)
            .WithSummary("创建推文")
            .WithDescription("发布一条新推文")
            .Accepts<CreateTweetRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        // POST /circle — 圈子发帖（发布即 Approved，仅圈子成员可见/可互动）
        group.MapPost("/circle", CreateCirclePostAsync)
            .WithSummary("圈子发帖")
            .WithDescription("发布到圈子：图文/视频/链接 + 话题关联；仅圈子成员可见、可互动")
            .Accepts<CreateCirclePostRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        // POST /draft — 保存草稿
        group.MapPost("/draft", SaveDraftAsync)
            .WithSummary("保存草稿")
            .WithDescription("保存一条推文草稿")
            .Accepts<CreateTweetRequest>("application/json")
            .Produces<ApiResponse<Guid>>();

        // GET /drafts — 我的草稿列表（字面量路由，先于 /{tweetGuid} 注册）
        group.MapGet("/drafts", GetMyDraftsAsync)
            .WithSummary("我的草稿列表")
            .WithDescription("获取当前用户的推文草稿列表（按最近编辑倒序），支持分页")
            .Produces<ApiResponse<PagedResult<TweetDto>>>();

        // GET /favorites/my — 我的收藏列表（字面量路由，先于 /{tweetGuid} 注册）
        group.MapGet("/favorites/my", GetMyFavoritesAsync)
            .WithSummary("我的收藏列表")
            .WithDescription("获取当前用户收藏的推文列表（按收藏时间倒序，含可见性过滤）")
            .Produces<ApiResponse<PagedResult<CommunityPostDto>>>();

        // GET /{tweetGuid} — 获取推文详情
        group.MapGet("/{tweetGuid}", GetTweetAsync)
            .WithSummary("获取推文详情")
            .WithDescription("根据推文ID获取推文详情，包含当前用户的交互状态")
            .Produces<ApiResponse<TweetDto>>();

        // GET /user/{userGuid} — 获取用户推文列表
        group.MapGet("/user/{userGuid}", GetUserTweetsAsync)
            .WithSummary("获取用户推文列表")
            .WithDescription("获取指定用户的推文列表，支持分页")
            .Produces<ApiResponse<PagedResult<TweetDto>>>();

        // GET /timeline — 获取时间线
        group.MapGet("/timeline", GetTimelineAsync)
            .WithSummary("获取时间线")
            .WithDescription("获取当前用户的时间线推文，支持分页")
            .Produces<ApiResponse<PagedResult<TweetDto>>>();

        // GET /trending — 获取趋势推文
        group.MapGet("/trending", GetTrendingAsync)
            .WithSummary("获取趋势推文")
            .WithDescription("获取热门趋势推文列表，支持分页")
            .Produces<ApiResponse<PagedResult<TweetDto>>>();

        // PUT /{tweetGuid} — 更新草稿
        group.MapPut("/{tweetGuid}", UpdateDraftAsync)
            .WithSummary("更新草稿")
            .WithDescription("更新指定的推文草稿")
            .Accepts<UpdateTweetRequest>("application/json")
            .Produces<ApiResponse>();

        // DELETE /{tweetGuid} — 删除推文
        group.MapDelete("/{tweetGuid}", DeleteTweetAsync)
            .WithSummary("删除推文")
            .WithDescription("删除指定的推文")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/pin — 置顶推文
        group.MapPost("/{tweetGuid}/pin", PinTweetAsync)
            .WithSummary("置顶推文")
            .WithDescription("将指定推文置顶")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/unpin — 取消置顶推文
        group.MapPost("/{tweetGuid}/unpin", UnpinTweetAsync)
            .WithSummary("取消置顶推文")
            .WithDescription("取消指定推文的置顶状态")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/like — 点赞推文
        group.MapPost("/{tweetGuid}/like", LikeTweetAsync)
            .WithSummary("点赞推文")
            .WithDescription("对指定推文进行点赞")
            .Produces<ApiResponse>();

        // DELETE /{tweetGuid}/like — 取消点赞
        group.MapDelete("/{tweetGuid}/like", UnlikeTweetAsync)
            .WithSummary("取消点赞")
            .WithDescription("取消对指定推文的点赞")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/favorite — 收藏推文
        group.MapPost("/{tweetGuid}/favorite", FavoriteTweetAsync)
            .WithSummary("收藏推文")
            .WithDescription("收藏指定推文")
            .Produces<ApiResponse>();

        // DELETE /{tweetGuid}/favorite — 取消收藏
        group.MapDelete("/{tweetGuid}/favorite", UnfavoriteTweetAsync)
            .WithSummary("取消收藏")
            .WithDescription("取消对指定推文的收藏")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/share — 分享推文
        group.MapPost("/{tweetGuid}/share", ShareTweetAsync)
            .WithSummary("分享推文")
            .WithDescription("分享指定推文")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/coin — 投币推文
        group.MapPost("/{tweetGuid}/coin", CoinTweetAsync)
            .WithSummary("投币推文")
            .WithDescription("对指定推文进行投币")
            .Produces<ApiResponse>();

        // POST /{tweetGuid}/view — 记录查看
        group.MapPost("/{tweetGuid}/view", RecordViewAsync)
            .WithSummary("记录查看")
            .WithDescription("记录用户查看了指定推文")
            .Produces<ApiResponse>();

        return group;
    }

    /// <summary>
    /// 创建推文（命令侧）。
    /// </summary>
    /// <param name="request">创建推文请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新推文 ID</returns>
    private static async Task<IResult> CreateTweetAsync(
        [FromBody] CreateTweetRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var tweetId = await mediator.SendAsync(
                new CreateTweetCommand(
                    userId,
                    request.Content,
                    request.FileIds,
                    request.LinkUrl,
                    ParseVisibility(request.Visibility)),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(tweetId, "推文创建成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"创建推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 圈子发帖（命令侧）：发布到圈子，免审核直接生效。
    /// </summary>
    private static async Task<IResult> CreateCirclePostAsync(
        [FromBody] CreateCirclePostRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var tweetId = await mediator.SendAsync(
                new CreateCirclePostCommand(
                    userId,
                    request.CircleGuid,
                    request.Content,
                    request.FileIds,
                    request.LinkUrl,
                    request.TopicGuids),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(tweetId, "圈子帖子发布成功"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Ok(ApiResponse<Guid>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Ok(ApiResponse<Guid>.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"圈子发帖失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 保存推文草稿（命令侧）。
    /// </summary>
    /// <param name="request">创建推文请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新草稿 ID</returns>
    private static async Task<IResult> SaveDraftAsync(
        [FromBody] CreateTweetRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var draftId = await mediator.SendAsync(
                new SaveDraftCommand(
                    userId,
                    request.Content,
                    request.FileIds,
                    request.LinkUrl,
                    ParseVisibility(request.Visibility)),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(draftId, "草稿保存成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"保存草稿失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取推文详情（查询侧，含当前用户的交互状态）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>推文 DTO</returns>
    private static async Task<IResult> GetTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var currentUserId = currentUser.GetUserId();
            var result = await mediator.SendAsync(new GetTweetDetailQuery(tweetGuid, currentUserId), ct);
            if (result.Tweet == null)
                return Results.Ok(ApiResponse<TweetDto>.NotFound("推文不存在"));

            var dto = MapToDto(result.Tweet, result.IsLiked, result.IsFavorited, result.IsCoined);
            return Results.Ok(ApiResponse<TweetDto>.Ok(dto));
        }
        catch (KeyNotFoundException)
        {
            return Results.Ok(ApiResponse<TweetDto>.NotFound("推文不存在"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<TweetDto>.Error($"获取推文详情失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户的草稿列表（查询侧，分页；R-08）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页草稿列表</returns>
    private static async Task<IResult> GetMyDraftsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetMyDraftsQuery(currentUser.GetUserId(), page, pageSize), ct);

            var result = new PagedResult<TweetDto>
            {
                Items = paged.Items.Select(t => MapToDto(t)).ToList(),
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<TweetDto>>.Error($"获取草稿列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetMyFavoritesAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetMyFavoritesQuery(currentUser.GetUserId(), page, pageSize), ct);
            return Results.Ok(ApiResponse<PagedResult<CommunityPostDto>>.Ok(paged));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<CommunityPostDto>>.Error($"获取收藏列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取指定用户的推文列表（查询侧，分页）。
    /// </summary>
    /// <param name="userGuid">用户ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页推文列表</returns>
    private static async Task<IResult> GetUserTweetsAsync(
        Guid userGuid,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetUserTweetsQuery(userGuid, page, pageSize), ct);

            var result = new PagedResult<TweetDto>
            {
                Items = paged.Items.Select(t => MapToDto(t)).ToList(),
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<TweetDto>>.Error($"获取用户推文列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前用户的时间线推文（查询侧，分页）。
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页时间线推文</returns>
    private static async Task<IResult> GetTimelineAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var paged = await mediator.SendAsync(new GetTimelineQuery(userId, page, pageSize), ct);

            var result = new PagedResult<TweetDto>
            {
                Items = paged.Items.Select(t => MapToDto(t)).ToList(),
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<TweetDto>>.Error($"获取时间线失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取热门趋势推文（查询侧，分页）。
    /// </summary>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分页趋势推文</returns>
    private static async Task<IResult> GetTrendingAsync(
        [FromServices] INotMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await mediator.SendAsync(new GetTrendingQuery(page, pageSize), ct);

            var result = new PagedResult<TweetDto>
            {
                Items = paged.Items.Select(t => MapToDto(t)).ToList(),
                TotalCount = paged.TotalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(ApiResponse<PagedResult<TweetDto>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<PagedResult<TweetDto>>.Error($"获取趋势推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 更新推文草稿（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="request">更新推文请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UpdateDraftAsync(
        Guid tweetGuid,
        [FromBody] UpdateTweetRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(
                new UpdateDraftCommand(
                    tweetGuid,
                    userId,
                    request.Content,
                    request.FileIds,
                    request.LinkUrl,
                    ParseVisibility(request.Visibility)),
                ct);

            return Results.Ok(ApiResponse.Ok("草稿更新成功"));
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
            return Results.Json(ApiResponse.Error($"更新草稿失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 删除推文（命令侧，仅限作者本人）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DeleteTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new DeleteTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("推文已删除"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 置顶推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> PinTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new PinTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("推文已置顶"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"置顶推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 取消置顶推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UnpinTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new UnpinTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("已取消置顶"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"取消置顶失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 点赞推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> LikeTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new LikeTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("点赞成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"点赞推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 取消点赞推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UnlikeTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new UnlikeTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("已取消点赞"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"取消点赞失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 收藏推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> FavoriteTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new FavoriteTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("收藏成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"收藏推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 取消收藏推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> UnfavoriteTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new UnfavoriteTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("已取消收藏"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"取消收藏失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 分享推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> ShareTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new ShareTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("分享成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"分享推文失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 对推文投币（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> CoinTweetAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new CoinTweetCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("投币成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"投币失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 记录用户查看了推文（命令侧）。
    /// </summary>
    /// <param name="tweetGuid">推文ID（路由参数）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> RecordViewAsync(
        Guid tweetGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            await mediator.SendAsync(new RecordTweetViewCommand(tweetGuid, userId), ct);
            return Results.Ok(ApiResponse.Ok("已记录查看"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"记录查看失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>推文实体 → DTO 映射</summary>
    private static TweetDto MapToDto(Tweet tweet, bool isLiked = false, bool isFavorited = false, bool isCoined = false) => new()
    {
        TweetGuid = tweet.TweetGuid,
        Author = new UserBriefDto
        {
            UserGuid = tweet.AuthorGuid,
            UserName = tweet.AuthorGuid.ToString("N")[..8]
        },
        Content = tweet.Content,
        MediaUrls = tweet.Media.Select(m => m.MediaUrl).ToList(),
        LinkMetadata = tweet.LinkMetadata is not null
            ? new LinkMetadataDto
            {
                Url = tweet.LinkMetadata.Url,
                Title = tweet.LinkMetadata.Title,
                Description = tweet.LinkMetadata.Description,
                Image = tweet.LinkMetadata.Image
            }
            : null,
        Hashtags = tweet.Hashtags.Any() ? tweet.Hashtags.ToList() : null,
        TweetStatus = tweet.TweetStatus.ToString(),
        Visibility = tweet.Visibility.ToString(),
        ViewCount = tweet.ViewCount,
        LikeCount = tweet.LikeCount,
        CommentCount = tweet.CommentCount,
        ShareCount = tweet.ShareCount,
        CoinCount = tweet.CoinCount,
        FavoriteCount = tweet.FavoriteCount,
        HotScore = tweet.HotScore,
        PublishTime = tweet.PublishTime,
        CreateTime = tweet.CreateTime,
        IsLiked = isLiked,
        IsFavorited = isFavorited,
        IsCoined = isCoined
    };

    /// <summary>将请求中的可见性字符串解析为枚举（与 Provider 解析逻辑保持一致）</summary>
    private static Visibility ParseVisibility(string? visibility) =>
        visibility?.ToLower() switch
        {
            "followers" => Visibility.Followers,
            "private" => Visibility.Private,
            _ => Visibility.Public
        };
}
