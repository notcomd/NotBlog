namespace Markdown.Web.API.APIs;

/// <summary>
///     Markdown 历史版本 API（列表 / 详情 / 删除 / 还原，挂文章组下）
/// </summary>
public static class MarkdownHistoryApi
{
    /// <summary>
    ///     注册历史版本端点：/api/markdown/{markDownGuid}/history/...
    /// </summary>
    public static RouteGroupBuilder MapMarkdownHistoryApi(this RouteGroupBuilder markdownGroup)
    {
        var historyGroup = markdownGroup.MapGroup("/{markDownGuid:guid}/history");

        // GET: 获取文档的所有历史版本（无需认证）
        historyGroup.MapGet("/", GetHistoryAsync)
            .Produces<ApiResponseResult<List<OldMarkDownResponse>>>(StatusCodes.Status200OK);

        // GET: 获取单个历史版本详情（无需认证）
        historyGroup.MapGet("/{oldMarkDownGuid:guid}", GetHistoryDetailAsync)
            .Produces<ApiResponseResult<OldMarkDownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound);

        // DELETE: 删除历史版本（需认证，软删除）
        historyGroup.MapDelete("/{oldMarkDownGuid:guid}", DeleteHistoryAsync)
            .RequireAuthorization()
            .Produces<ApiResponseResult>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 从历史版本还原（仅作者）
        historyGroup.MapPost("/{oldMarkDownGuid:guid}/restore", RestoreAsync)
            .RequireAuthorization()
            .Produces<ApiResponseResult>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponseResult>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return historyGroup;
    }

    /// <summary>
    ///     获取文档的所有历史版本
    /// </summary>
    private static async Task<IResult> GetHistoryAsync(
        Guid markDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        // 越权防护：权限校验 + 审核门控双重要求（与 GetAsync 一致），
        // 防止草稿文档的历史版本被匿名读取
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);
        var viewerGuid = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService) ?? Guid.Empty;
        if (markdown is null || markdown.IsDelete ||
            !markdown.HasPermission(viewerGuid) ||
            (!markdown.IsApproved && markdown.MarkUserGuid != viewerGuid))
            return Results.NotFound(ApiResponseResult<List<OldMarkDownResponse>>.NotFound("文章不存在"));

        var oldVersions = await markdownRepository.GetOldMarkDownsByMarkDownGuidAsync(markDownGuid);
        var responses = oldVersions.Select(OldMarkDownMapper.MapToOldMarkDownResponse).ToList();
        return Results.Ok(ApiResponseResult<List<OldMarkDownResponse>>.Ok(responses));
    }

    /// <summary>
    ///     获取单个历史版本详情
    /// </summary>
    private static async Task<IResult> GetHistoryDetailAsync(
        Guid oldMarkDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        var oldVersion = await markdownRepository.GetOldMarkDownByGuidAsync(oldMarkDownGuid);

        if (oldVersion is null || oldVersion.IsDelete)
            return Results.NotFound(ApiResponseResult<OldMarkDownResponse>.NotFound("历史版本不存在"));

        // 越权防护：权限校验 + 审核门控双重要求（与 GetAsync 一致）
        var markdown = await markdownRepository.FindMarkDownAsync(oldVersion.MarkDownGuid);
        var viewerGuid = MarkdownApiHelpers.TryGetCurrentUserId(currentUserService) ?? Guid.Empty;
        if (markdown is null || markdown.IsDelete ||
            !markdown.HasPermission(viewerGuid) ||
            (!markdown.IsApproved && markdown.MarkUserGuid != viewerGuid))
            return Results.NotFound(ApiResponseResult<OldMarkDownResponse>.NotFound("历史版本不存在"));

        var response = OldMarkDownMapper.MapToOldMarkDownResponse(oldVersion);
        return Results.Ok(ApiResponseResult<OldMarkDownResponse>.Ok(response));
    }

    /// <summary>
    ///     删除历史版本（软删除）
    /// </summary>
    private static async Task<IResult> DeleteHistoryAsync(
        Guid oldMarkDownGuid,
        [FromServices]IMarkdownRepository markdownRepository,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();

        // 越权防护：历史版本仅所有者可删除
        var oldVersion = await markdownRepository.GetOldMarkDownByGuidAsync(oldMarkDownGuid)
            ?? throw new KeyNotFoundException($"历史版本不存在：{oldMarkDownGuid}");

        if (oldVersion.UserGuid != userId)
            throw new UnauthorizedAccessException("无权删除此历史版本");

        await markdownRepository.DeleteOldMarkDownAsync(oldMarkDownGuid);
        await markdownRepository.UnitOfWork.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(ApiResponseResult.Ok("历史版本已删除"));
    }

    /// <summary>
    ///     从历史版本还原（仅作者）
    /// </summary>
    private static async Task<IResult> RestoreAsync(
        Guid markDownGuid,
        Guid oldMarkDownGuid,
        [FromServices]INotMediator notMediator,
        [FromServices]ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();
        var result = await notMediator.SendAsync(new RestoreMarkdownCommand(markDownGuid, oldMarkDownGuid, userId));

        return result
            ? Results.Ok(ApiResponseResult.Ok("历史版本已还原"))
            : Results.StatusCode(500);
    }
}
