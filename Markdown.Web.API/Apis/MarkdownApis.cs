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

        // GET: 获取文章详情（无需认证）
        markdownGroup.MapGet("/{markDownGuid:guid}", GetAsync)
            .Produces<ApiResponse<MarkdownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

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

        // ===== MarkReview 评论端点（嵌套在文档下） =====
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

        // ===== OldMarkDown 历史版本端点 =====
        var historyGroup = markdownGroup.MapGroup("/{markDownGuid:guid}/history");

        // GET: 获取文档的所有历史版本（无需认证）
        historyGroup.MapGet("/", GetHistoryAsync)
            .Produces<ApiResponse<List<OldMarkDownResponse>>>(StatusCodes.Status200OK);

        // GET: 获取单个历史版本详情（无需认证）
        historyGroup.MapGet("/{oldMarkDownGuid:guid}", GetHistoryDetailAsync)
            .Produces<ApiResponse<OldMarkDownResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        // DELETE: 删除历史版本（需认证，软删除）
        historyGroup.MapDelete("/{oldMarkDownGuid:guid}", DeleteHistoryAsync)
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    // ==================== MarkDown 端点处理 ====================

    /// <summary>
    /// 创建 Markdown 博客文章
    /// </summary>
    private static async Task<IResult> CreateAsync(
        [FromBody] CreateMarkdownRequest request,
        INotMediator notMediator,
        ICurrentUserService currentUserService,
        IMarkdownRepository markdownRepository)
    {
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

            var markdowns = await markdownRepository.FindMarkDownsAsync(userId);
            var created = markdowns?.FirstOrDefault(x => x.MarkDownName == request.Name);

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
    /// 获取公开 Markdown 文章列表（分页）
    /// </summary>
    private static async Task<IResult> GetAllAsync(
        IMarkdownRepository markdownRepository,
        int skip = 0,
        int take = 20)
    {
        var markdowns = await markdownRepository.FindAllMarkDownsAsync(skip, take);
        var responses = markdowns.Select(MapToResponse).ToList();
        return Results.Ok(ApiResponse<List<MarkdownResponse>>.Ok(responses));
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
    /// 删除 Markdown 文章（软删除）
    /// </summary>
    private static async Task<IResult> DeleteAsync(
        Guid markDownGuid,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        try
        {
            var userId = currentUserService.GetUserId();
            var command = new DeleteMarkdownCommand(markDownGuid, userId, Guid.CreateVersion7());

            var result = await notMediator.SendAsync(command);

            return result
                ? Results.Ok(ApiResponse.Ok("文章已删除"))
                : Results.StatusCode(500);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(
                ApiResponse.Error(ex.Message),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    // ==================== MarkReview 评论端点处理 ====================

    /// <summary>
    /// 创建评论
    /// </summary>
    private static async Task<IResult> CreateReviewAsync(
        Guid markDownGuid,
        [FromBody] CreateMarkReviewRequest request,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("评论内容不能为空"));

        try
        {
            var userId = currentUserService.GetUserId();
            var reviewAuth = ParseReviewAuth(request.Auth);

            var command = new CreateMarkReviewCommand(
                markDownGuid,
                userId,
                request.Content,
                ReviewImages: request.ReviewImages,
                ReviewAuth: reviewAuth,
                IdempotencyKey: Guid.CreateVersion7());

            var reviewGuid = await notMediator.SendAsync(command);

            return Results.Created(
                $"/api/markdown/{markDownGuid}/reviews/detail/{reviewGuid}",
                ApiResponse<Guid>.Created(reviewGuid, "评论创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Error(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Json(ApiResponse.Error("用户认证失败"), statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// 获取文档的所有顶级评论
    /// </summary>
    private static async Task<IResult> GetReviewsAsync(
        Guid markDownGuid,
        IMarkdownRepository markdownRepository)
    {
        var reviews = await markdownRepository.GetReviewsByMarkdownIdAsync(markDownGuid);
        var responses = reviews.Select(MapToReviewResponse).ToList();
        return Results.Ok(ApiResponse<List<MarkReviewResponse>>.Ok(responses));
    }

    /// <summary>
    /// 获取单条评论详情
    /// </summary>
    private static async Task<IResult> GetReviewAsync(
        Guid reviewGuid,
        IMarkdownRepository markdownRepository)
    {
        var review = await markdownRepository.GetReviewByIdAsync(reviewGuid);

        if (review is null || review.IsDelete)
            return Results.NotFound(ApiResponse<MarkReviewResponse>.NotFound("评论不存在"));

        var response = MapToReviewResponse(review);
        return Results.Ok(ApiResponse<MarkReviewResponse>.Ok(response));
    }

    /// <summary>
    /// 获取评论的子评论
    /// </summary>
    private static async Task<IResult> GetChildReviewsAsync(
        Guid reviewGuid,
        IMarkdownRepository markdownRepository)
    {
        var childReviews = await markdownRepository.GetChildReviewsAsync(reviewGuid);
        var responses = childReviews.Select(MapToReviewResponse).ToList();
        return Results.Ok(ApiResponse<List<MarkReviewResponse>>.Ok(responses));
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    private static async Task<IResult> UpdateReviewAsync(
        Guid reviewGuid,
        [FromBody] UpdateMarkReviewRequest request,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(ApiResponse.Error("评论内容不能为空"));

        try
        {
            var userId = currentUserService.GetUserId();
            var command = new UpdateMarkReviewCommand(reviewGuid, userId, request.Content, Guid.CreateVersion7());

            var result = await notMediator.SendAsync(command);

            return result
                ? Results.Ok(ApiResponse.Ok("评论更新成功"))
                : Results.StatusCode(500);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(
                ApiResponse.Error(ex.Message),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// 删除评论（软删除）
    /// </summary>
    private static async Task<IResult> DeleteReviewAsync(
        Guid reviewGuid,
        INotMediator notMediator,
        ICurrentUserService currentUserService)
    {
        try
        {
            var userId = currentUserService.GetUserId();
            var command = new DeleteMarkReviewCommand(reviewGuid, userId, Guid.CreateVersion7());

            var result = await notMediator.SendAsync(command);

            return result
                ? Results.Ok(ApiResponse.Ok("评论已删除"))
                : Results.StatusCode(500);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Error(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(
                ApiResponse.Error(ex.Message),
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    // ==================== OldMarkDown 历史版本端点处理 ====================

    /// <summary>
    /// 获取文档的所有历史版本
    /// </summary>
    private static async Task<IResult> GetHistoryAsync(
        Guid markDownGuid,
        IMarkdownRepository markdownRepository)
    {
        var oldVersions = await markdownRepository.GetOldMarkDownsByMarkDownGuidAsync(markDownGuid);
        var responses = oldVersions.Select(OldMarkDownMapper.MapToOldMarkDownResponse).ToList();
        return Results.Ok(ApiResponse<List<OldMarkDownResponse>>.Ok(responses));
    }

    /// <summary>
    /// 获取单个历史版本详情
    /// </summary>
    private static async Task<IResult> GetHistoryDetailAsync(
        Guid oldMarkDownGuid,
        IMarkdownRepository markdownRepository)
    {
        var oldVersion = await markdownRepository.GetOldMarkDownByGuidAsync(oldMarkDownGuid);

        if (oldVersion is null || oldVersion.IsDelete)
            return Results.NotFound(ApiResponse<OldMarkDownResponse>.NotFound("历史版本不存在"));

        var response = OldMarkDownMapper.MapToOldMarkDownResponse(oldVersion);
        return Results.Ok(ApiResponse<OldMarkDownResponse>.Ok(response));
    }

    /// <summary>
    /// 删除历史版本（软删除）
    /// </summary>
    private static async Task<IResult> DeleteHistoryAsync(
        Guid oldMarkDownGuid,
        IMarkdownRepository markdownRepository)
    {
        try
        {
            await markdownRepository.DeleteOldMarkDownAsync(oldMarkDownGuid);
            await markdownRepository.UnitOfWork.SavaChangesAsync(CancellationToken.None);
            return Results.Ok(ApiResponse.Ok("历史版本已删除"));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Error(ex.Message));
        }
    }

    // ==================== 映射工具方法 ====================

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
    /// 解析评论权限类型
    /// </summary>
    private static MarkReviewAuth ParseReviewAuth(string? auth)
    {
        if (string.IsNullOrWhiteSpace(auth))
            return MarkReviewAuth.ReviewAuthPublic;

        return auth.ToLowerInvariant() switch
        {
            "public" => MarkReviewAuth.ReviewAuthPublic,
            "private" => MarkReviewAuth.ReviewAuthPrivate,
            "protected" => MarkReviewAuth.ReviewAuthProtected,
            _ => MarkReviewAuth.ReviewAuthPublic
        };
    }

    /// <summary>
    /// 将 MarkDown 实体映射为响应 DTO
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

    /// <summary>
    /// 将 MarkReview 实体映射为响应 DTO
    /// </summary>
    private static MarkReviewResponse MapToReviewResponse(MarkReview review) => new()
    {
        MarkReviewGuid = review.MarkReviewGuid,
        MarkDownGuid = review.MarkDownGuid,
        UserId = review.UserId,
        Content = review.MarkReviewContent ?? string.Empty,
        Auth = review.MarkReviewAuth.ToString(),
        ReviewTime = review.MarkReviewTime,
        IsDeleted = review.IsDelete,
        ChildReviewCount = review.MarkReviews?.Count ?? 0,
        Quote = review.MarkQuote is not null ? MapToQuoteResponse(review.MarkQuote) : null
    };

    /// <summary>
    /// 将 MarkQuote 值对象映射为响应 DTO
    /// </summary>
    private static MarkQuoteResponse MapToQuoteResponse(MarkQuote quote) => new()
    {
        LoveCount = quote.LoveSome,
        ReplyCount = quote.ReviewSome,
        CommentCount = quote.CommentSome,
        ShareCount = quote.ShareSome,
        ViewCount = quote.ViewSome,
        TotalInteractions = quote.GetTotalInteractions()
    };
}

/// <summary>
/// OldMarkDown 历史版本响应 DTO
/// </summary>
public class OldMarkDownResponse
{
    public Guid OldMarkDownGuid { get; set; }
    public Guid MarkDownGuid { get; set; }
    public Guid UserGuid { get; set; }
    public string Status { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTimeOffset CreateAt { get; set; }
    public DateTimeOffset UpdateAt { get; set; }
}

/// <summary>
/// OldMarkDown 响应映射扩展
/// </summary>
file static class OldMarkDownMapper
{
    public static OldMarkDownResponse MapToOldMarkDownResponse(OldMarkDown old)
    {
        return new OldMarkDownResponse
        {
            OldMarkDownGuid = old.OldMarkDownGuid,
            MarkDownGuid = old.MarkDownGuid,
            UserGuid = old.UserGuid,
            Status = old.Status.ToString(),
            Content = old.OldMarkDownContent,
            Hash = old.OldMarkDownHash,
            CreateAt = old.CreateAt,
            UpdateAt = old.UpdateAt
        };
    }
}
