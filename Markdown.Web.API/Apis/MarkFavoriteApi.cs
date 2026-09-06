namespace Markdown.Web.API.Apis;

/// <summary>
///     Markdown 文章收藏 API（用户维度资源：/api/favorites，全部需认证）
/// </summary>
public static class MarkFavoriteApi
{
    /// <summary>
    ///     注册收藏端点：添加 / 取消 / 列表（tag 分类过滤 + 分页）
    /// </summary>
    public static void MapMarkFavoriteApi(this WebApplication app)
    {
        var favoriteGroup = app.MapGroup("/api/favorites")
            .RequireAuthorization()
            .RequireResourcePermissions("api:favorite");

        // POST: 添加收藏（已收藏时合并标签，幂等）
        favoriteGroup.MapPost("/", AddFavoriteAsync)
            .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // DELETE: 取消收藏（幂等：未收藏也返回成功）
        favoriteGroup.MapDelete("/{markDownGuid:guid}", RemoveFavoriteAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // PUT: 覆盖式更新收藏标签（需认证）
        favoriteGroup.MapPut("/{markDownGuid:guid}/tags", UpdateTagsAsync)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 我的标签库（标签复用建议，按常用/最近使用排序，支持关键字过滤）
        favoriteGroup.MapGet("/tags", GetTagsAsync)
            .Produces<ApiResponse<List<MarkFavoriteTagResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 我的收藏列表（tag 分类快速查找 + 分页）
        favoriteGroup.MapGet("/", GetFavoritesAsync)
            .Produces<ApiResponse<List<MarkFavoriteResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    ///     添加收藏
    /// </summary>
    private static async Task<IResult> AddFavoriteAsync(
        [FromBody] AddFavoriteRequest request,
        [FromServices]INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
      [FromServices]  IMarkdownRepository markdownRepository,
      [FromServices]  HttpContext httpContext)
    {
        if (request.MarkDownGuid == Guid.Empty)
            return Results.BadRequest(ApiResponse.Error("文章标识不能为空"));
        if (!MarkdownApiHelpers.TryValidateTags(request.Tags, out var tagError))
            return Results.BadRequest(ApiResponse.Error(tagError ?? "标签校验失败"));

        var userId = currentUserService.GetUserId();

        // 越权防护（P1-2）：文章不可见（私有/未过审/已删除）时一律 404，与读侧门控一致
        var markdown = await markdownRepository.FindMarkDownAsync(request.MarkDownGuid);
        if (markdown is null || markdown.IsDelete ||
            (!markdown.IsApproved && markdown.MarkUserGuid != userId) ||
            !markdown.HasPermission(userId))
            return Results.NotFound(ApiResponse<Guid>.NotFound("文章不存在"));

        var favoriteGuid = await  notMediator.SendAsync(new AddFavoriteCommand(
            userId,
            request.MarkDownGuid,
            request.Tags,
            MarkdownApiHelpers.GetIdempotencyKey(httpContext)));

        return Results.Created($"/api/favorites/{request.MarkDownGuid}",
            ApiResponse<Guid>.Created(favoriteGuid, "收藏成功"));
    }

    /// <summary>
    ///     取消收藏（幂等）
    /// </summary>
    private static async Task<IResult> RemoveFavoriteAsync(
        Guid markDownGuid,
       [FromServices] INotMediator notMediator,
      [FromServices]  ICurrentUserService currentUserService,
      [FromServices]  HttpContext httpContext)
    {
        var userId = currentUserService.GetUserId();
        var result = await  notMediator.SendAsync(new RemoveFavoriteCommand(
            userId,
            markDownGuid,
            MarkdownApiHelpers.GetIdempotencyKey(httpContext)));

        return result
            ? Results.Ok(ApiResponse.Ok("已取消收藏"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     覆盖式更新收藏标签（收藏不存在返回 404）
    /// </summary>
    private static async Task<IResult> UpdateTagsAsync(
        Guid markDownGuid,
        [FromBody] UpdateFavoriteTagsRequest request,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService,
       [FromServices] HttpContext httpContext)
    {
        if (!MarkdownApiHelpers.TryValidateTags(request.Tags, out var tagError))
            return Results.BadRequest(ApiResponse.Error(tagError ?? "标签校验失败"));

        var userId = currentUserService.GetUserId();
        var result = await  notMediator.SendAsync(new UpdateFavoriteTagsCommand(
            userId,
            markDownGuid,
            request.Tags,
            MarkdownApiHelpers.GetIdempotencyKey(httpContext)));

        return result
            ? Results.Ok(ApiResponse.Ok("标签已更新"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     我的标签库（标签复用建议：常用优先，支持关键字过滤）
    /// </summary>
    private static async Task<IResult> GetTagsAsync(
        string? keyword,
        int? limit,
        [FromServices]INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        var result = await  notMediator.SendAsync(new MarkFavoriteTagsQuery(
            userId,
            Keyword: keyword,
            Limit: limit is > 0 and <= 100 ? limit.Value : 50));

        return Results.Ok(ApiResponse<List<MarkFavoriteTagResponse>>.Ok(result));
    }

    /// <summary>
    ///     我的收藏列表（tag 分类快速查找 + 分页）
    /// </summary>
    private static async Task<IResult> GetFavoritesAsync(
        string? tag,
        int? skip,
        int? take,
       [FromServices] INotMediator notMediator,
       [FromServices] ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        var query = new MarkFavoriteListQuery(
            userId,
            Tag: tag,
            Skip: Math.Max(0, skip ?? 0),
            Take: take is > 0 and <= 100 ? take.Value : 20);

        var result = await notMediator.SendAsync(query);
        return Results.Ok(ApiResponse<List<MarkFavoriteResponse>>.Ok(result));
    }
}
