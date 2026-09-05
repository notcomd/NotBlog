namespace Markdown.Web.API.Apis;

/// <summary>
///     Markdown 博客文章 API（文章主资源：CRUD + 列表 + 搜索）
/// </summary>
public static class MarkdownApi
{
    /// <summary>
    ///     注册全部 Markdown 端点（文章 / 审核 / 评论 / 历史版本）
    /// </summary>
    public static void MapMarkdownApis(this WebApplication app)
    {
        // ===== MarkDown 文档端点 =====
        var markdownGroup = app.MapGroup("/api/markdown");

        // POST: 创建文章（需认证）
        markdownGroup.MapPost("/", CreateAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 获取文章列表（无需认证，仅返回公开文档）
        markdownGroup.MapGet("/", GetAllAsync)
            .Produces<ApiResponse<List<MarkdownResponse>>>(StatusCodes.Status200OK);

        // GET: 获取文章详情（无需认证，元数据 + 交互统计，不含正文）
        markdownGroup.MapGet("/{markDownGuid:guid}", GetAsync)
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // GET: 获取文章正文（文件化存储，流式返回；权限校验与详情一致）
        markdownGroup.MapGet("/{markDownGuid:guid}/content", GetContentAsync)
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // POST: 文档浏览 +1（无需认证，公开文档可匿名浏览）
        markdownGroup.MapPost("/{markDownGuid:guid}/view", AddViewAsync)
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // POST: 文档点赞（需认证，同一用户仅可点赞一次）
        markdownGroup.MapPost("/{markDownGuid:guid}/like", LikeDocumentAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 取消文档点赞（需认证，未点赞时幂等返回）
        markdownGroup.MapPost("/{markDownGuid:guid}/unlike", UnlikeDocumentAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 文档分享 +1（需认证）
        markdownGroup.MapPost("/{markDownGuid:guid}/share", ShareDocumentAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 文档打赏硬币（需认证，数量 1~100）
        markdownGroup.MapPost("/{markDownGuid:guid}/coin", CoinDocumentAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // PUT: 更新文章（需认证）
        markdownGroup.MapPut("/{markDownGuid:guid}", UpdateAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // DELETE: 删除文章（需认证，软删除）
        markdownGroup.MapDelete("/{markDownGuid:guid}", DeleteAsync)
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 文章列表（分页/按标签/按用户，摘要投影，仅已审核通过）
        markdownGroup.MapGet("/list", GetListAsync)
            .Produces<ApiResponse<List<MarkdownSummaryResponse>>>(StatusCodes.Status200OK);

        // GET: 文章搜索（摘要投影，仅已审核通过）
        markdownGroup.MapGet("/search", SearchAsync)
            .Produces<ApiResponse<List<MarkdownSummaryResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);

        // GET: 热点榜（Redis ZSet 直读，miss 单飞重建；Redis 故障降级 DB 计算）
        markdownGroup.MapGet("/hot", GetHotBoardAsync)
            .Produces<ApiResponse<List<MarkdownHotResponse>>>(StatusCodes.Status200OK);

        // ===== 子资源：审核 / 评论 / 历史版本 =====
        MarkdownAuditApi.MapMarkdownAuditApi(markdownGroup);
        MarkdownReviewApi.MapMarkdownReviewApi(markdownGroup);
        MarkdownHistoryApi.MapMarkdownHistoryApi(markdownGroup);
    }

    /// <summary>
    ///     创建 Markdown 博客文章
    /// </summary>
    private static async Task<IResult> CreateAsync(
        [FromBody] CreateMarkdownRequest request,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
        [FromServices]IMarkdownRepository markdownRepository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(ApiResponse.Error("文章名称不能为空"));
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("文章内容不能为空"));
        if (!MarkdownApiHelpers.TryValidateTags(request.Tags, out var tagError))
            return Results.BadRequest(ApiResponse.Error(tagError ?? "标签校验失败"));

        var userId = currentUserService.GetUserId();
        var auth = MarkdownApiHelpers.ParseAuth(request.Auth);
        if (auth is null)
            return Results.BadRequest(ApiResponse.Error("非法的文章权限类型"));

        var command = new CreateMarkdownCommand(
            userId,
            request.Name,
            request.Content,
            Tags: request.Tags,
            MarkDownAuth: auth.Value,
            CoverUrl: request.CoverUrl);

        // P1-4：命令直接返回新文章 Guid，避免全量加载用户文章再按名称匹配（低效且同名歧义）
        var markDownGuid = await notMediator.SendAsync(command);

        var created = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (created is null)
            return Results.StatusCode(201);

        var response = MarkdownResponseMapper.MapToMarkdownResponse(created);
        return Results.Created($"/api/markdown/{created.MarkDownGuid}",
            ApiResponse<MarkdownResponse>.Created(response, "文章创建成功"));
    }

    /// <summary>
    ///     获取公开 Markdown 文章列表（分页）
    /// </summary>
    private static async Task<IResult> GetAllAsync(
       [FromServices] IMarkdownRepository markdownRepository,
        int skip = 0,
        int take = 20)
    {
        // 资源限制（P-05）：skip 不允许为负，take 钳制在 1~100；
        // 列表为摘要投影（正文已文件化，列表不加载正文文件）
        var markdowns = await markdownRepository.FindAllMarkDownsAsync(
            Math.Max(0, skip),
            Math.Clamp(take, 1, 100));
        var responses = markdowns
            .Select(m => new MarkdownSummaryResponse
            {
                MarkDownGuid = m.MarkDownGuid,
                Name = m.MarkDownName,
                Tags = [.. m.MarkDownTagboard],
                CoverUrl = m.CoverUrl,
                Auth = m.MarkDownAuth.ToString(),
                Status = m.Status.ToString(),
                CreateAt = m.CreateAt,
                UpdateAt = m.UpdateAt
            })
            .ToList();
        return Results.Ok(ApiResponse<List<MarkdownSummaryResponse>>.Ok(responses));
    }

    /// <summary>
    ///     热点榜 TopN（Redis ZSet 直读；miss 单飞重建；Redis 故障降级 DB 实时计算）
    /// </summary>
    private static async Task<IResult> GetHotBoardAsync(
        int take,
        [FromServices]IMarkdownHotBoardService hotBoardService)
    {
        var takeClamped = Math.Clamp(take <= 0 ? 20 : take, 1, 100);
        var items = await hotBoardService.GetHotBoardAsync(takeClamped);

        var response = items
            .Select(i => new MarkdownHotResponse
            {
                MarkDownGuid = i.MarkDownGuid,
                Name = i.Name,
                HeatScore = i.HeatScore,
                CreateAt = i.CreateAt
            })
            .ToList();

        return Results.Ok(ApiResponse<List<MarkdownHotResponse>>.Ok(response));
    }

    /// <summary>
    ///     文档浏览 +1（公开文档可匿名浏览；私有/未过审文档仅作者可见时允许）。
    ///     已登录用户 24h 窗口 Redis Set 防刷（匿名无标识不防刷；Redis 不可用时跳过防刷）
    /// </summary>
    private static async Task<IResult> AddViewAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownHotBoardService hotBoardService,
        [FromServices]ICurrentUserService currentUserService,
        [FromServices]IServiceProvider serviceProvider)
    {
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete)
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        var viewerGuid = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService) ?? Guid.Empty;
        if (!markdown.HasPermission(viewerGuid) ||
            (!markdown.IsApproved && markdown.MarkUserGuid != viewerGuid))
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        // 防刷（阶段 3）：已登录用户 24h 窗口去重；新 Set 自动设 TTL 防膨胀
        var userId = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService);
        if (userId.HasValue)
        {
            var redis = serviceProvider.GetService<CacheMemory.Core.IRedisCacheService>();
            if (redis is not null)
            {
                var dedupKey = $"markdown:viewed:{markDownGuid:N}";
                var isFirstView = await redis.SetAddAsync(dedupKey, userId.Value.ToString("N"));
                if (isFirstView)
                {
                    await redis.KeyExpireAsync(dedupKey, TimeSpan.FromHours(24));
                }
                else
                {
                    // 重复浏览：返回当前计数，不递增
                    return Results.Ok(ApiResponse<long>.Ok(markdown.MarkQuote.ViewSome));
                }
            }
        }

        var count = await markdownRepository.IncreaseDocumentViewAsync(markDownGuid);

        // 热度分实时刷新（失败不影响浏览计数，定时重建兜底）
        await hotBoardService.UpdateScoreAsync(markDownGuid);
        return Results.Ok(ApiResponse<long>.Ok(count));
    }

    /// <summary>
    ///     文档点赞 +1（同一用户仅可点赞一次）
    /// </summary>
    private static async Task<IResult> LikeDocumentAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownHotBoardService hotBoardService,
        [FromServices]ICurrentUserService currentUserService,
        [FromServices]IEventBus eventBus,
        [FromServices]ILoggerFactory loggerFactory)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护：文档不可见（已删除/私有/未过审）一律 404
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        var result = await markdownRepository.LikeDocumentAsync(markDownGuid, userId);

        // 仅首次点赞发布作者通知（重复点赞幂等分支不发，防止轰炸）
        if (result.IsFirst)
        {
            await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownInteractionIntegrationEvent(
                MarkdownInteractionType.DocumentLiked,
                markDownGuid, markdown.MarkDownName, ReviewGuid: null,
                ActorUserId: userId, TargetUserId: markdown.MarkUserGuid,
                Amount: 0, OccurredAt: DateTimeOffset.UtcNow),
                loggerFactory.CreateLogger("MarkdownApi.LikeDocument"));
        }

        // 热度分实时刷新（失败不影响点赞，定时重建兜底）
        await hotBoardService.UpdateScoreAsync(markDownGuid);
        return Results.Ok(ApiResponse<long>.Ok(result.Count, "点赞成功"));
    }

    /// <summary>
    ///     取消文档点赞 -1（未点赞时幂等返回当前计数）
    /// </summary>
    private static async Task<IResult> UnlikeDocumentAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownHotBoardService hotBoardService,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护：与点赞一致
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        var count = await markdownRepository.RemoveLikeDocumentAsync(markDownGuid, userId);

        // 热度分实时刷新（失败不影响取消点赞，定时重建兜底）
        await hotBoardService.UpdateScoreAsync(markDownGuid);
        return Results.Ok(ApiResponse<long>.Ok(count, "已取消点赞"));
    }

    /// <summary>
    ///     文档分享 +1
    /// </summary>
    private static async Task<IResult> ShareDocumentAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownHotBoardService hotBoardService,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护：与点赞一致
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        var count = await markdownRepository.AddDocumentShareAsync(markDownGuid);

        // 热度分实时刷新（失败不影响分享，定时重建兜底）
        await hotBoardService.UpdateScoreAsync(markDownGuid);
        return Results.Ok(ApiResponse<long>.Ok(count, "分享成功"));
    }

    /// <summary>
    ///     文档打赏硬币（一用户一篇仅一次；重复投币幂等返回现总额，不重复累计、不再发通知）
    /// </summary>
    private static async Task<IResult> CoinDocumentAsync(
        Guid markDownGuid,
        [FromBody] CoinMarkdownRequest request,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownHotBoardService hotBoardService,
        [FromServices]ICurrentUserService currentUserService,
        [FromServices]IEventBus eventBus,
        [FromServices]ILoggerFactory loggerFactory)
    {
        var userId = currentUserService.GetUserId();

        if (request.Amount is < 1 or > 100)
            return Results.BadRequest(ApiResponse.Error("打赏数量必须在 1~100 之间"));

        // 越权防护：与点赞一致
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("文章不存在"));

        var result = await markdownRepository.CoinDocumentAsync(markDownGuid, userId, request.Amount);

        // 仅首次投币发布作者通知（重复投币幂等分支不发）
        if (result.IsFirst)
        {
            await EventPublishing.PublishSafelyAsync(eventBus, new MarkdownInteractionIntegrationEvent(
                MarkdownInteractionType.DocumentCoined,
                markDownGuid, markdown.MarkDownName, ReviewGuid: null,
                ActorUserId: userId, TargetUserId: markdown.MarkUserGuid,
                Amount: request.Amount, OccurredAt: DateTimeOffset.UtcNow),
                loggerFactory.CreateLogger("MarkdownApi.CoinDocument"));
        }

        // 热度分实时刷新（失败不影响打赏，定时重建兜底）
        await hotBoardService.UpdateScoreAsync(markDownGuid);
        return Results.Ok(ApiResponse<long>.Ok(result.Count, result.IsFirst ? "打赏成功" : "您已打赏过该文章，不重复累计"));
    }

    /// <summary>
    ///     获取 Markdown 文章正文（文件化存储：从内容存储读取，权限校验与详情一致）
    /// </summary>
    private static async Task<IResult> GetContentAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]IMarkdownContentStore contentStore,
        [FromServices] ICurrentUserService currentUserService)
    {
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);

        if (markdown is null || markdown.IsDelete)
            return Results.NotFound(ApiResponse<string>.NotFound("文章不存在"));

        // 越权防护（与 GetAsync 一致）：权限校验 + 审核门控双重要求
        var viewerGuid = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService) ?? Guid.Empty;
        if (!markdown.HasPermission(viewerGuid) ||
            (!markdown.IsApproved && markdown.MarkUserGuid != viewerGuid))
            return Results.NotFound(ApiResponse<string>.NotFound("文章不存在"));

        var content = await contentStore.ReadAsync(markdown.FileId);
        if (content is null)
            return Results.NotFound(ApiResponse<string>.NotFound("文章正文文件不存在"));

        return Results.Ok(ApiResponse<string>.Ok(content));
    }

    /// <summary>
    ///     获取 Markdown 博客文章详情
    /// </summary>
    private static async Task<IResult> GetAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
       [FromServices] ICurrentUserService currentUserService)
    {
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);

        if (markdown is null || markdown.IsDelete)
            return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound("文章不存在"));

        // 越权防护（S-10）：权限校验 + 审核门控（F-10.2）双重要求。
        // 私有/受保护文档非所有者一律 404（含已审核通过的私有文档）；
        // 未通过审核的文章仅作者可见，其余一律 404。
        var viewerGuid = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService) ?? Guid.Empty;
        if (!markdown.HasPermission(viewerGuid) ||
            (!markdown.IsApproved && markdown.MarkUserGuid != viewerGuid))
            return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound("文章不存在"));

        var response = MarkdownResponseMapper.MapToMarkdownResponse(markdown);
        return Results.Ok(ApiResponse<MarkdownResponse>.Ok(response));
    }

    /// <summary>
    ///     更新 Markdown 博客文章
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid markDownGuid,
        [FromBody] UpdateMarkdownRequest request,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] IMarkdownRepository markdownRepository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(ApiResponse.Error("文章名称不能为空"));
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("文章内容不能为空"));
        if (!MarkdownApiHelpers.TryValidateTags(request.Tags, out var tagError))
            return Results.BadRequest(ApiResponse.Error(tagError ?? "标签校验失败"));

        var userId = currentUserService.GetUserId();

        var command = new UpdateMarkdownCommand(
            markDownGuid,
            userId,
            request.Name,
            request.Content,
            Tags: request.Tags,
            CoverUrl: request.CoverUrl);

        var result = await notMediator.SendAsync(command);

        if (!result)
            return Results.StatusCode(500);

        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);

        if (markdown is null || markdown.IsDelete)
            return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound("文章不存在"));

        var response = MarkdownResponseMapper.MapToMarkdownResponse(markdown);
        return Results.Ok(ApiResponse<MarkdownResponse>.Ok(response, "文章更新成功"));
    }

    /// <summary>
    ///     删除 Markdown 文章（软删除）
    /// </summary>
    private static async Task<IResult> DeleteAsync(
        Guid markDownGuid,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] HttpContext httpContext)
    {
        var userId = currentUserService.GetUserId();
        var command = new DeleteMarkdownCommand(markDownGuid, userId, MarkdownApiHelpers.GetIdempotencyKey(httpContext));

        var result = await  notMediator.SendAsync(command);

        return result
            ? Results.Ok(ApiResponse.Ok("文章已删除"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     获取文章列表（分页/按标签/按用户，摘要投影，仅已审核通过）
    /// </summary>
    private static async Task<IResult> GetListAsync(
        int? skip,
        int? take,
        string? tag,
        Guid? userGuid,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService)
    {
        var query = new MarkdownListQuery(
            Skip: Math.Max(0, skip ?? 0),
            Take: take is > 0 and <= 100 ? take.Value : 20,
            Tag: tag,
            UserGuid: userGuid,
            ViewerGuid: MarkdownApiHelpers.TryGetCurrentUserId(currentUserService));

        var result = await notMediator.SendAsync(query);
        return Results.Ok(ApiResponse<List<MarkdownSummaryResponse>>.Ok(result));
    }

    /// <summary>
    ///     搜索文章（摘要投影，仅已审核通过）
    /// </summary>
    private static async Task<IResult> SearchAsync(
        string? keyword,
        int? skip,
        int? take,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Results.BadRequest(ApiResponse.Error("搜索关键字不能为空"));

        var query = new MarkdownSearchQuery(
            keyword.Trim(),
            Skip: Math.Max(0, skip ?? 0),
            Take: take is > 0 and <= 100 ? take.Value : 20,
            ViewerGuid: MarkdownApiHelpers.TryGetCurrentUserId(currentUserService));

        var result = await notMediator.SendAsync(query);
        return Results.Ok(ApiResponse<List<MarkdownSummaryResponse>>.Ok(result));
    }
}
