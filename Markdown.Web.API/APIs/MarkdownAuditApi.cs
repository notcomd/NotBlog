namespace Markdown.Web.API.APIs;

/// <summary>
///     Markdown 文章审核 API（提交审核 / 通过 / 驳回）
/// </summary>
public static class MarkdownAuditApi
{
    /// <summary>
    ///     注册审核端点（挂在文章组下：/api/markdown/{markDownGuid}/submit|approve|reject）
    /// </summary>
    public static RouteGroupBuilder MapMarkdownAuditApi(this RouteGroupBuilder markdownGroup)
    {
        // POST: 提交审核（仅作者，草稿/驳回 -> 待审核）
        markdownGroup.MapPost("/{markDownGuid:guid}/submit", SubmitAsync)
            .RequireAuthorization()
            .Produces<ApiResponseResult>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponseResult>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 审核通过（仅作者/管理员）
        markdownGroup.MapPost("/{markDownGuid:guid}/approve", ApproveAsync)
            .RequireAuthorization()
            .Produces<ApiResponseResult>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponseResult>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        // POST: 审核驳回（仅作者/管理员）
        markdownGroup.MapPost("/{markDownGuid:guid}/reject", RejectAsync)
            .RequireAuthorization()
            .Produces<ApiResponseResult>(StatusCodes.Status200OK)
            .Produces<ApiResponseResult>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponseResult>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponseResult>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return markdownGroup;
    }

    /// <summary>
    ///     提交审核（草稿/驳回 -> 待审核，仅作者）
    /// </summary>
    private static async Task<IResult> SubmitAsync(
        Guid markDownGuid,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();
        var result = await  notMediator.SendAsync(new SubmitMarkdownCommand(markDownGuid, userId));

        return result
            ? Results.Ok(ApiResponseResult.Ok("文章已提交审核"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     审核通过（待审核 -> 通过，仅作者/管理员）
    /// </summary>
    private static async Task<IResult> ApproveAsync(
        Guid markDownGuid,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();
        var result = await   notMediator.SendAsync(
            new ApproveMarkdownCommand(markDownGuid, userId, MarkdownApiHelpers.IsAdmin(currentUserService)));

        return result
            ? Results.Ok(ApiResponseResult.Ok("文章审核通过"))
            : Results.StatusCode(500);
    }

    /// <summary>
    ///     审核驳回（待审核 -> 驳回，仅作者/管理员）
    /// </summary>
    private static async Task<IResult> RejectAsync(
        Guid markDownGuid,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();
        var result = await  notMediator.SendAsync(
            new RejectMarkdownCommand(markDownGuid, userId, MarkdownApiHelpers.IsAdmin(currentUserService)));

        return result
            ? Results.Ok(ApiResponseResult.Ok("文章审核驳回"))
            : Results.StatusCode(500);
    }
}
