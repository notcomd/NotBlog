namespace Markdown.Web.API.Apis;

/// <summary>
///     Markdown 评论 API（顶级评论 / 子评论 / 点赞，挂文章组下）
/// </summary>
public static class MarkdownReviewApi
{
    /// <summary>
    ///     注册评论端点：/api/markdown/{markDownGuid}/reviews/...
    /// </summary>
    public static void MapMarkdownReviewApi(this RouteGroupBuilder markdownGroup)
    {
        var reviewGroup = markdownGroup.MapGroup("/{markDownGuid:guid}/reviews");

        // POST: 创建评论（需认证）
        reviewGroup.MapPost("/", CreateReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<MarkReviewResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 获取文档的所有顶级评论（无需认证）
        reviewGroup.MapGet("/", GetReviewsAsync)
            .Produces<ApiResponse<List<MarkReviewResponse>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // GET: 获取单条评论详情（无需认证）
        reviewGroup.MapGet("/detail/{reviewGuid:guid}", GetReviewAsync)
            .Produces<ApiResponse<MarkReviewResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // GET: 获取评论的子评论（无需认证）
        reviewGroup.MapGet("/{reviewGuid:guid}/children", GetChildReviewsAsync)
            .Produces<ApiResponse<List<MarkReviewResponse>>>(StatusCodes.Status200OK);

        // PUT: 更新评论（需认证）
        reviewGroup.MapPut("/{reviewGuid:guid}", UpdateReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // DELETE: 删除评论（需认证，软删除）
        reviewGroup.MapDelete("/{reviewGuid:guid}", DeleteReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 添加子评论（父评论 Guid + 内容，F-10.4）
        reviewGroup.MapPost("/{reviewGuid:guid}/children", AddChildReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 评论点赞/取消点赞（计数接线 F-10.5）
        reviewGroup.MapPost("/{reviewGuid:guid}/like", LikeReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        reviewGroup.MapPost("/{reviewGuid:guid}/unlike", UnlikeReviewAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<long>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    ///     创建评论
    /// </summary>
    private static async Task<IResult> CreateReviewAsync(
        Guid markDownGuid,
        [FromBody] CreateMarkReviewRequest request,
       [FromServices] INotMediator notMediator,
        [FromServices]ICurrentUserService currentUserService,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("评论内容不能为空"));

        var reviewImages = MarkdownApiHelpers.MapReviewImages(request.ReviewImages);

        var userId = currentUserService.GetUserId();

        // 越权防护（P1-2）：文档不可见（私有/未过审/已删除）时一律 404，与读侧门控一致
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<Guid>.NotFound("文章不存在"));

        var reviewAuth = MarkdownApiHelpers.ParseReviewAuth(request.Auth);
        if (reviewAuth is null)
            return Results.BadRequest(ApiResponse.Error("非法的评论权限类型"));

        var command = new CreateMarkReviewCommand(
            markDownGuid,
            userId,
            request.Content,
            ReviewImages: reviewImages,
            ReviewAuth: reviewAuth.Value,
            IdempotencyKey: MarkdownApiHelpers.GetIdempotencyKey(httpContext));

        var reviewGuid = await  notMediator.SendAsync(command);

        return Results.Created(
            $"/api/markdown/{markDownGuid}/reviews/detail/{reviewGuid}",
            ApiResponse<Guid>.Created(reviewGuid, "评论创建成功"));
    }

    /// <summary>
    ///     获取文档的所有顶级评论
    /// </summary>
    private static async Task<IResult> GetReviewsAsync(
        Guid markDownGuid,
       [FromServices] IMarkdownRepository markdownRepository,
       [FromServices] ICurrentUserService currentUserService)
    {
        var userId = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService);

        // 越权防护：文档不可读时（私有文档非所有者/已删除）一律 404
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != (userId ?? Guid.Empty)) ||
            !markdown.HasPermission(userId ?? Guid.Empty))
            return Results.NotFound(ApiResponse<List<MarkReviewResponse>>.NotFound("文章不存在"));

        var reviews = await markdownRepository.GetReviewsByMarkdownIdAsync(markDownGuid);
        var responses = reviews
            .Where(r => MarkdownApiHelpers.IsReviewVisible(r, userId))
            .Select(MarkReviewResponseMapper.MapToMarkReviewResponse)
            .ToList();
        return Results.Ok(ApiResponse<List<MarkReviewResponse>>.Ok(responses));
    }

    /// <summary>
    ///     获取单条评论详情
    /// </summary>
    private static async Task<IResult> GetReviewAsync(
        Guid reviewGuid,
       [FromServices] IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        var review = await markdownRepository.GetReviewByIdAsync(reviewGuid);

        if (review is null || review.IsDelete)
            return Results.NotFound(ApiResponse<MarkReviewResponse>.NotFound("评论不存在"));

        var userId = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService);

        // 越权防护：评论所属文档不可读或评论本身不可见（私有评论非所有者）一律 404
        var markdown = await markdownRepository.FindMarkDownAsync(review.MarkDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != (userId ?? Guid.Empty)) ||
            !markdown.HasPermission(userId ?? Guid.Empty))
            return Results.NotFound(ApiResponse<MarkReviewResponse>.NotFound("评论不存在"));

        if (!MarkdownApiHelpers.IsReviewVisible(review, userId))
            return Results.NotFound(ApiResponse<MarkReviewResponse>.NotFound("评论不存在"));

        // 浏览量计数接线（F-10.5）：读取评论详情时浏览数 +1（ExecuteUpdate 原子更新，无需 SaveChanges）
        await markdownRepository.IncreaseReviewViewAsync(reviewGuid);

        var response = MarkReviewResponseMapper.MapToMarkReviewResponse(review);
        return Results.Ok(ApiResponse<MarkReviewResponse>.Ok(response));
    }

    /// <summary>
    ///     获取评论的子评论
    /// </summary>
    private static async Task<IResult> GetChildReviewsAsync(
        Guid reviewGuid,
       [FromServices] IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService);

        // 越权防护：父评论及其所属文档需对当前用户可见
        var parent = await markdownRepository.GetReviewByIdAsync(reviewGuid);
        if (parent is null || parent.IsDelete)
            return Results.NotFound(ApiResponse<List<MarkReviewResponse>>.NotFound("评论不存在"));

        var markdown = await markdownRepository.FindMarkDownAsync(parent.MarkDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != (userId ?? Guid.Empty)) ||
            !markdown.HasPermission(userId ?? Guid.Empty))
            return Results.NotFound(ApiResponse<List<MarkReviewResponse>>.NotFound("评论不存在"));

        if (!MarkdownApiHelpers.IsReviewVisible(parent, userId))
            return Results.NotFound(ApiResponse<List<MarkReviewResponse>>.NotFound("评论不存在"));

        var childReviews = await markdownRepository.GetChildReviewsAsync(reviewGuid);
        var responses = childReviews
            .Where(r => MarkdownApiHelpers.IsReviewVisible(r, userId))
            .Select(MarkReviewResponseMapper.MapToMarkReviewResponse)
            .ToList();
        return Results.Ok(ApiResponse<List<MarkReviewResponse>>.Ok(responses));
    }

    /// <summary>
    ///     更新评论
    /// </summary>
    private static async Task<IResult> UpdateReviewAsync(
        Guid reviewGuid,
        [FromBody] UpdateMarkReviewRequest request,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("评论内容不能为空"));

        var userId = currentUserService.GetUserId();
        var command = new UpdateMarkReviewCommand(reviewGuid, userId, request.Content,
            MarkdownApiHelpers.GetIdempotencyKey(httpContext));

        var result = await notMediator.SendAsync(command);

        return result
            ? Results.Ok(ApiResponse.Ok("评论更新成功"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     删除评论（软删除）
    /// </summary>
    private static async Task<IResult> DeleteReviewAsync(
        Guid reviewGuid,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] HttpContext httpContext)
    {
        var userId = currentUserService.GetUserId();
        var command = new DeleteMarkReviewCommand(reviewGuid, userId, MarkdownApiHelpers.GetIdempotencyKey(httpContext));

        var result = await notMediator.SendAsync(command);

        return result
            ? Results.Ok(ApiResponse.Ok("评论已删除"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     添加子评论（父评论 Guid + 内容，F-10.4）
    /// </summary>
    private static async Task<IResult> AddChildReviewAsync(
        Guid markDownGuid,
        Guid reviewGuid,
        [FromBody] CreateMarkReviewRequest request,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] IMarkdownRepository markdownRepository,
       [FromServices] HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("评论内容不能为空"));

        var reviewImages = MarkdownApiHelpers.MapReviewImages(request.ReviewImages);

        var userId = currentUserService.GetUserId();

        // 越权防护（P1-2）：文档不可见（私有/未过审/已删除）时一律 404，与读侧门控一致
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<Guid>.NotFound("文章不存在"));

        var reviewAuth = MarkdownApiHelpers.ParseReviewAuth(request.Auth);
        if (reviewAuth is null)
            return Results.BadRequest(ApiResponse.Error("非法的评论权限类型"));

        var command = new AddChildReviewCommand(
            markDownGuid,
            reviewGuid,
            userId,
            request.Content,
            ReviewImages: reviewImages,
            ReviewAuth: reviewAuth.Value,
            IdempotencyKey: MarkdownApiHelpers.GetIdempotencyKey(httpContext));

        var childGuid = await notMediator.SendAsync(command);

        return Results.Created(
            $"/api/markdown/{markDownGuid}/reviews/{reviewGuid}/children/{childGuid}",
            ApiResponse<Guid>.Created(childGuid, "子评论创建成功"));
    }

    /// <summary>
    ///     评论点赞 +1（F-10.5，同一用户仅可点赞一次）
    /// </summary>
    private static async Task<IResult> LikeReviewAsync(
        Guid reviewGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService,
        [FromServices]IEventBus eventBus,
        [FromServices]ILoggerFactory loggerFactory)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护（P1-2）：评论不可见（已删除）或所属文档不可读（私有/未过审）一律 404
        var review = await markdownRepository.GetReviewByIdAsync(reviewGuid);
        if (review is null || review.IsDelete)
            return Results.NotFound(ApiResponse<long>.NotFound("评论不存在"));

        var markdown = await markdownRepository.FindMarkDownAsync(review.MarkDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("评论不存在"));

        var count = await markdownRepository.LikeReviewAsync(reviewGuid, userId);

        // 发布点赞集成事件（总线故障不拖垮业务，P1-6）
        await EventPublishing.PublishSafelyAsync(eventBus, new MarkReviewLikedIntegrationEvent(
            reviewGuid, review.MarkDownGuid, userId, count, DateTimeOffset.UtcNow),
            loggerFactory.CreateLogger("MarkdownReviewApi.LikeReview"));

        return Results.Ok(ApiResponse<long>.Ok(count, "点赞成功"));
    }

    /// <summary>
    ///     取消评论点赞 -1（F-10.5，未点赞时幂等返回）
    /// </summary>
    private static async Task<IResult> UnlikeReviewAsync(
        Guid reviewGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护（P1-2）：与点赞一致——评论不可见或所属文档不可读一律 404
        var review = await markdownRepository.GetReviewByIdAsync(reviewGuid);
        if (review is null || review.IsDelete)
            return Results.NotFound(ApiResponse<long>.NotFound("评论不存在"));

        var markdown = await markdownRepository.FindMarkDownAsync(review.MarkDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<long>.NotFound("评论不存在"));

        var count = await markdownRepository.RemoveLikeReviewAsync(reviewGuid, userId);
        return Results.Ok(ApiResponse<long>.Ok(count, "已取消点赞"));
    }
}
