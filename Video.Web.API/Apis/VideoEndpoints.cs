
using Commons.Result;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频接口 — 查询视频列表 + 视频点赞（CQRS + 幂等性）
/// </summary>
public static class VideoEndpoints
{
    public static RouteGroupBuilder MapVideoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/video")
            .RequireResourcePermissions("api:video")
            .WithTags("Video");

        // --- GET ---
        group.MapGet("/", GetByVideoListAsync)
            .WithName("GetVideoList")
            .WithDescription("Get all videos");

        group.MapGet("/{index:int}/{pageSize:int}", GetByVideoPage)
            .WithName("GetVideoPage")
            .WithDescription("Get videos by page");

        group.MapGet("/findname/{videoName}", GetByVideoNameAsync)
            .WithName("GetVideoByName")
            .WithDescription("Find a video by exact name");

        group.MapGet("/blurred/{videoName}", BlurredByVideoAsync)
            .WithName("BlurredVideoSearch")
            .WithDescription("Fuzzy search videos by name");

        // --- GET /mine — 我的视频（可选状态过滤；字面量路由先于参数路由注册） ---
        group.MapGet("/mine", GetMyVideosAsync)
            .WithName("GetMyVideos")
            .WithDescription("Get current user's own videos, optionally filtered by status")
            .RequireAuthorization();

        // --- POST /{videoGuid}/submit — 作者本人提交审核（Draft|Rejected → Pending） ---
        group.MapPost("/{videoGuid:guid}/submit", SubmitVideoForReviewAsync)
            .WithName("SubmitVideoForReview")
            .WithDescription("Submit a video for review (author only)")
            .RequireAuthorization();

        // --- PUT ---
        group.MapPut("/", UpdateByVideoAsync)
            .WithName("UpdateVideo")
            .WithDescription("Update video information")
            .RequireAuthorization();

        // --- DELETE 视频（F-08.1：仅作者可删，调用者由服务端解析） ---
        group.MapDelete("/{videoGuid:guid}", DeleteVideoAsync)
            .WithName("DeleteVideo")
            .WithDescription("Delete a video (author only)")
            .RequireAuthorization();

        // --- POST 视频点赞 ---
        group.MapPost("/{videoGuid:guid}/like", LikeVideoAsync)
            .WithName("LikeVideo")
            .WithDescription("Like/unlike a video (upvote/down/ballot/share)")
            .RequireAuthorization();

        return group;
    }

    /// <summary>
    /// 获取视频列表（按查看者身份收敛可见范围）：
    /// 匿名/他人仅「已审核通过 + 公开 + 未删除」；作者本人可见自己的全部状态；管理员可见全部。
    /// </summary>
    private static async Task<IResult> GetByVideoListAsync(
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var viewerGuid = currentUser.IsAuthenticated ? currentUser.GetUserId() : (Guid?)null;
        var videoModel = await videoServiceDI.VideoRepository.FindByVideoListAsync(viewerGuid, currentUser.IsAdmin());
        return Results.Ok(videoModel);
    }

    /// <summary>
    /// 根据分页获取视频
    /// </summary>
    private static async Task<IResult> GetByVideoPage(int index, int pageSize, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.PageByVideoAsync(index, pageSize);
        return Results.Ok(videoModel);
    }

    /// <summary>
    /// 根据视频名称获取视频
    /// </summary>
    private static async Task<IResult> GetByVideoNameAsync(string videoName, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.FindByVideoName(videoName);
        return Results.Ok(videoModel);
    }

    /// <summary>
    /// 模糊搜索视频
    /// </summary>
    private static async Task<IResult> BlurredByVideoAsync(string videoName, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.BlurredByVideoName(videoName);
        return Results.Ok(videoModel);
    }

    /// <summary>
    /// 更新视频信息（作者本人；受编辑门禁约束：草稿/被驳回可编辑，待审核/已通过不可编辑）。
    /// </summary>
    private static async Task<IResult> UpdateByVideoAsync(
        [FromBody] RequestUpdateByVideo updateVideo,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        if (updateVideo is null)
        {
            videoServiceDI.Logger.LogError("updateVideo is null");
            return Results.Json(
                ApiResponseResult<string>.Failure("Request body is null.", 400),
                statusCode: 400);
        }

        Videos videoModel;
        try
        {
            videoModel = await videoServiceDI.VideoRepository.FindByVideoAsync(updateVideo.VideoGuid);
        }
        catch (AggregateException)
        {
            return Results.Json(
                ApiResponseResult<string>.Failure("Video not found.", 404),
                statusCode: 404);
        }

        // 作者校验：调用者必须属于视频的 Affiliated 集合（服务端解析的当前用户，不信任客户端传入的 Guid）
        var callerGuid = currentUser.GetUserId();
        if (callerGuid == Guid.Empty || videoModel.Affiliated is null || !videoModel.Affiliated.Contains(callerGuid))
            return Results.Json(
                ApiResponseResult<string>.Failure("Unauthorized update attempt.", 403),
                statusCode: 403);

        try
        {
            // 编辑门禁在领域层强制：草稿/被驳回可编辑；待审核/已通过不可编辑（抛 InvalidOperationException）
            videoModel.UpdateContent(updateVideo.VideoName, updateVideo.VideoCover, updateVideo.VideoFileUri,
                updateVideo.BriefIntroduction, updateVideo.Tags.ToList());
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 400),
                statusCode: 400);
        }

        await videoServiceDI.VideoRepository.UnitOfWork.SaveEntitiesAsync();
        await videoServiceDI.VideoCacheService.InvalidateVideoAsync(videoModel.VideoGuid);

        return Results.Ok("UP!");
    }

    /// <summary>
    /// 删除视频（F-08.1：仅作者可删；调用者由服务端 ICurrentUserService 解析，不信任客户端 Guid）
    /// </summary>
    private static async Task<IResult> DeleteVideoAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            var command = new DeleteVideoCommand(videoGuid, callerGuid);
            var deleted = await videoServiceDI.NotMediator.SendAsync(command);

            if (!deleted)
                return Results.Json(
                    ApiResponseResult<string>.Failure("You are not authorized to delete this video, or it does not exist.", 403),
                    statusCode: 403);

            logger.LogInformation("Video {VideoGuid} deleted by user {UserGuid}", videoGuid, callerGuid);
            return Results.NoContent();
        }
        catch (AggregateException ex)
        {
            // DeleteVideoCommandHandler 通过 FindByVideoWithDetailsAsync 加载视频，
            // 视频不存在时该仓储方法抛 AggregateException。
            logger.LogWarning(ex, "Video {VideoGuid} not found for deletion", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure("Video not found.", 404),
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 视频点赞/取消点赞（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> LikeVideoAsync(
        Guid videoGuid,
        [FromBody] VideoLikeRequest request,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            // S-18.2：点赞用户由服务端从 JWT 解析，忽略客户端传入的 UserGuid，防伪造上报
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            var validFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "upvote", "down", "ballot", "share" };

            if (!validFields.Contains(request.Field))
                return Results.Json(
                    ApiResponseResult<string>.Failure($"Invalid field '{request.Field}'. Valid: upvote, down, ballot, share.", 400),
                    statusCode: 400);

            var command = new LikeVideoCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: callerGuid,
                Field: request.Field,
                IsLike: request.IsLike);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    ApiResponseResult<string>.Failure(result.ErrorMessage ?? "Failed to like video.", 400),
                    statusCode: 400);

            logger.LogInformation("Video {VideoGuid}: {Field} like operation, NewCount={Count}",
                videoGuid, request.Field, result.NewCount);

            return Results.Ok(new { Field = request.Field, NewCount = result.NewCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to like video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 我的视频列表（当前登录用户自己的视频，可按状态过滤；status 省略表示全部状态）。
    /// </summary>
    private static async Task<IResult> GetMyVideosAsync(
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!TryParseVideoStatus(status, out var parsedStatus, out var error))
            return Results.Json(
                ApiResponseResult<VideoPagedResult<MyVideoDto>>.Failure(error, 400),
                statusCode: 400);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var callerGuid = currentUser.GetUserId();
        var items = await videoServiceDI.VideoRepository.PageByAuthorAsync(callerGuid, parsedStatus, page, pageSize);
        var total = await videoServiceDI.VideoRepository.CountByAuthorAsync(callerGuid, parsedStatus);

        var result = new VideoPagedResult<MyVideoDto>
        {
            Items = items.Select(MapToMyVideoDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(ApiResponseResult<VideoPagedResult<MyVideoDto>>.Ok(result));
    }

    /// <summary>
    /// 提交视频审核（作者本人；Draft|Rejected → Pending）。
    /// 非作者 403，视频不存在 404，状态不合法 400。
    /// </summary>
    private static async Task<IResult> SubmitVideoForReviewAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            await videoServiceDI.NotMediator.SendAsync(new SubmitVideoForReviewCommand(videoGuid, callerGuid));

            logger.LogInformation("Video {VideoGuid} submitted for review by {UserGuid}", videoGuid, callerGuid);
            return Results.Ok(ApiResponseResult.Ok("视频已提交审核"));
        }
        catch (AggregateException ex)
        {
            // FindByVideoWithDetailsAsync 在视频不存在时抛 AggregateException
            logger.LogWarning(ex, "Video {VideoGuid} not found for submit", videoGuid);
            return Results.Json(
                ApiResponseResult.Failure("Video not found.", 404),
                statusCode: 404);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(ApiResponseResult.Forbidden(ex.Message), statusCode: 403);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult.Failure(ex.Message, 400), statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit video {VideoGuid} for review", videoGuid);
            return Results.Json(ApiResponseResult.Error(ex.Message), statusCode: 500);
        }
    }

    /// <summary>视频实体 → 我的视频/审核列表 DTO 映射。</summary>
    private static MyVideoDto MapToMyVideoDto(Videos video) => new(
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

/// <summary>视频点赞请求 DTO。</summary>
public record VideoLikeRequest(Guid UserGuid, string Field, bool IsLike = true);
