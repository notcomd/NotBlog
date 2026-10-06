using Commons.Result;
using NotMediator;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频审核接口（管理端） — 待审/按状态列表 + 通过/驳回，仅管理员可调用。
/// <para>
/// 落点说明：项目内管理端审核统一置于各服务的 audit 前缀下（见 Message 服务 <c>/api/audit/*</c>）。
/// 本服务沿用 Video 路由前缀与权限资源，采用 <c>/api/video/audit/*</c>，与现有 <c>Apis/</c> 组织一致。
/// 管理员判定统一走 <see cref="ICurrentUserService.IsAdmin"/>（内部即 <c>HasAdminRole()</c> 口径）。
/// </para>
/// </summary>
public static class VideoAuditEndpoints
{
    public static RouteGroupBuilder MapVideoAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/video/audit")
            .RequireResourcePermissions("api:video")
            .WithTags("VideoAudit")
            .RequireAuthorization();

        // GET /list — 按状态列表（status 省略表示全部状态；管理端审核列表）
        group.MapGet("/list", GetAuditVideosAsync)
            .WithName("GetAuditVideos")
            .WithDescription("Get videos by status for admin review (status omitted = all)")
            .Produces<ApiResponseResult<VideoPagedResult<MyVideoDto>>>();

        // POST /{videoGuid}/approve — 通过审核（Pending → Approved）
        group.MapPost("/{videoGuid:guid}/approve", ApproveVideoAsync)
            .WithName("ApproveVideo")
            .WithDescription("Approve a pending video (admin only)");

        // POST /{videoGuid}/reject — 驳回（Pending → Rejected，body 含 Reason）
        group.MapPost("/{videoGuid:guid}/reject", RejectVideoAsync)
            .WithName("RejectVideo")
            .WithDescription("Reject a pending video with a reason (admin only)");

        return group;
    }

    /// <summary>管理端按状态分页查询视频（仅管理员）。</summary>
    private static async Task<IResult> GetAuditVideosAsync(
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!currentUser.IsAdmin())
            return Results.Json(ApiResponseResult.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

        if (!TryParseVideoStatus(status, out var parsedStatus, out var error))
            return Results.Json(
                ApiResponseResult<VideoPagedResult<MyVideoDto>>.Failure(error, 400),
                statusCode: 400);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var items = await videoServiceDI.VideoRepository.PageByStatusAsync(parsedStatus, page, pageSize);
        var total = await videoServiceDI.VideoRepository.CountByStatusAsync(parsedStatus);

        var result = new VideoPagedResult<MyVideoDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(ApiResponseResult<VideoPagedResult<MyVideoDto>>.Ok(result));
    }

    /// <summary>通过视频审核（仅管理员；Pending → Approved）。</summary>
    private static async Task<IResult> ApproveVideoAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        if (!currentUser.IsAdmin())
            return Results.Json(ApiResponseResult.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

        try
        {
            await videoServiceDI.NotMediator.SendAsync(new ApproveVideoCommand(videoGuid));
            logger.LogInformation("Video {VideoGuid} approved by {AdminGuid}", videoGuid, currentUser.GetUserId());
            return Results.Ok(ApiResponseResult.Ok("视频已通过审核"));
        }
        catch (AggregateException ex)
        {
            logger.LogWarning(ex, "Video {VideoGuid} not found for approve", videoGuid);
            return Results.Json(ApiResponseResult.NotFound("Video not found."), statusCode: 404);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult.Failure(ex.Message, 400), statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to approve video {VideoGuid}", videoGuid);
            return Results.Json(ApiResponseResult.Error(ex.Message), statusCode: 500);
        }
    }

    /// <summary>驳回视频（仅管理员；Pending → Rejected，写入驳回原因）。</summary>
    private static async Task<IResult> RejectVideoAsync(
        Guid videoGuid,
        [FromBody] RequestRejectVideo request,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        if (!currentUser.IsAdmin())
            return Results.Json(ApiResponseResult.Forbidden("仅管理员可执行审核操作"), statusCode: 403);

        try
        {
            await videoServiceDI.NotMediator.SendAsync(new RejectVideoCommand(videoGuid, request.Reason));
            logger.LogInformation("Video {VideoGuid} rejected by {AdminGuid}: {Reason}",
                videoGuid, currentUser.GetUserId(), request.Reason);
            return Results.Ok(ApiResponseResult.Ok("视频已驳回"));
        }
        catch (AggregateException ex)
        {
            logger.LogWarning(ex, "Video {VideoGuid} not found for reject", videoGuid);
            return Results.Json(ApiResponseResult.NotFound("Video not found."), statusCode: 404);
        }
        catch (ArgumentException ex)
        {
            return Results.Json(ApiResponseResult.Failure(ex.Message, 400), statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult.Failure(ex.Message, 400), statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reject video {VideoGuid}", videoGuid);
            return Results.Json(ApiResponseResult.Error(ex.Message), statusCode: 500);
        }
    }

    /// <summary>视频实体 → 审核列表 DTO 映射。</summary>
    private static MyVideoDto MapToDto(Videos video) => new(
        video.VideoGuid,
        video.VideoName,
        video.BriefIntroduction,
        video.VideoCover?.ToString(),
        video.Status.ToString(),
        video.RejectReason,
        video.TimeSpace.CreateAt,
        video.VideoControl.AuthorVideo.ToString());

    /// <summary>解析视频状态查询参数（省略/空表示全部状态；非法值返回错误）。</summary>
    private static bool TryParseVideoStatus(string? status, out VideoStatus? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(status))
            return true;

        if (Enum.TryParse<VideoStatus>(status, ignoreCase: true, out var value))
        {
            parsed = value;
            return true;
        }

        error = "无效的状态值，允许值：Draft、Pending、Approved、Rejected";
        return false;
    }
}
