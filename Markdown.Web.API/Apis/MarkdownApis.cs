using Markdown.Web.API.Application.Commands;
using Markdown.Web.API.Application.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Markdown.Web.API.Apis;

/// <summary>
/// Markdown 博客文章管理 Minimal API
/// </summary>
public static class MarkdownApis
{
    public static void MapMarkdownApis(this WebApplication app)
    {
        var group = app.MapGroup("/api/markdown");

        // POST: 创建文章（需认证）
        group.MapPost("/", CreateAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET: 获取文章详情（无需认证）
        group.MapGet("/{markDownGuid:guid}", GetAsync)
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // PUT: 更新文章（需认证）
        group.MapPut("/{markDownGuid:guid}", UpdateAsync)
            .RequireAuthorization()
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// 创建 Markdown 博客文章
    /// </summary>
    private static async Task<IResult> CreateAsync(
        [FromBody] CreateMarkdownRequest request,
        INotMediator notMediator,
        ICurrentUserService currentUserService,
        IMarkdownRepository markdownRepository)
    {
        // 手动验证
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(ApiResponse.Error("文章名称不能为空"));
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("文章内容不能为空"));

        try
        {
            var userId = currentUserService.GetUserId();
            var auth = ParseAuth(request.Auth);

            var command = new CreateMarkdownCommand(
                userId,
                request.Name,
                request.Content,
                Tags: request.Tags,
                MarkDownAuth: auth);

            var result = await notMediator.SendAsync(command);

            if (!result)
                return Results.StatusCode(500);

            // 查询刚创建的文章
            var markdown = await markdownRepository.FindMarkDownsAsync(userId);

            var created = markdown?.FirstOrDefault(x => x.MarkDownName == request.Name);

            if (created is null)
                return Results.StatusCode(201);

            var response = MapToResponse(created);
            return Results.Created($"/api/markdown/{created.MarkDownGuid}",
                ApiResponse<MarkdownResponse>.Created(response, "文章创建成功"));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Json(ApiResponse.Error("用户认证失败"), statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// 获取 Markdown 博客文章详情
    /// </summary>
    private static async Task<IResult> GetAsync(
        Guid markDownGuid,
        IMarkdownRepository markdownRepository)
    {
        var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);

        if (markdown is null || markdown.IsDelete)
            return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound("文章不存在"));

        var response = MapToResponse(markdown);
        return Results.Ok(ApiResponse<MarkdownResponse>.Ok(response));
    }

    /// <summary>
    /// 更新 Markdown 博客文章
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid markDownGuid,
        [FromBody] UpdateMarkdownRequest request,
        INotMediator notMediator,
        ICurrentUserService currentUserService,
        IMarkdownRepository markdownRepository)
    {
        // 手动验证
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(ApiResponse.Error("文章名称不能为空"));
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("文章内容不能为空"));

        try
        {
            var userId = currentUserService.GetUserId();

            var command = new UpdateMarkdownCommand(
                markDownGuid,
                userId,
                request.Name,
                request.Content,
                Tags: request.Tags);

            var result = await notMediator.SendAsync(command);

            if (!result)
                return Results.StatusCode(500);

            // 查询更新后的文章
            var markdown = await markdownRepository.FindMarkDownAsync(markDownGuid);

            if (markdown is null || markdown.IsDelete)
                return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound("文章不存在"));

            var response = MapToResponse(markdown);
            return Results.Ok(ApiResponse<MarkdownResponse>.Ok(response, "文章更新成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse<MarkdownResponse>.NotFound(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(
                ApiResponse<MarkdownResponse>.Forbidden(ex.Message),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 解析文章权限类型
    /// </summary>
    private static MarkDownAuth ParseAuth(string? auth)
    {
        if (string.IsNullOrWhiteSpace(auth))
            return MarkDownAuth.PublicMark;

        return auth.ToLowerInvariant() switch
        {
            "public" => MarkDownAuth.PublicMark,
            "private" => MarkDownAuth.PrivateMark,
            "protected" => MarkDownAuth.ProtectedMark,
            "admin" => MarkDownAuth.AdminMark,
            "root" => MarkDownAuth.RootMark,
            _ => MarkDownAuth.PublicMark
        };
    }

    /// <summary>
    /// 将实体映射为响应 DTO
    /// </summary>
    private static MarkdownResponse MapToResponse(MarkDown markdown) => new()
    {
        MarkDownGuid = markdown.MarkDownGuid,
        Name = markdown.MarkDownName,
        Content = markdown.MarkDownContent,
        Hash = markdown.MarkDownHash,
        Tags = [.. markdown.MarkDownTagboard],
        Auth = markdown.MarkDownAuth.ToString(),
        CreateAt = markdown.CreateAt,
        UpdateAt = markdown.UpdateAt
    };
}
