
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
    /// 获取所有视频
    /// </summary>
    private static async Task<IResult> GetByVideoListAsync([FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.FindByVideoListAsync();
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
    /// 更新视频信息
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

        var videoModel = await videoServiceDI.VideoRepository.FindByVideoAsync(updateVideo.VideoGuid);

        // 作者校验：调用者必须属于视频的 Affiliated 集合（服务端解析的当前用户，不信任客户端传入的 Guid）
        var callerGuid = currentUser.GetUserId();
        if (callerGuid == Guid.Empty || videoModel.Affiliated is null || !videoModel.Affiliated.Contains(callerGuid))
            return Results.Json(
                ApiResponseResult<string>.Failure("Unauthorized update attempt.", 403),
                statusCode: 403);

        // 修正参数错赋：VideoFileUri 与 VideoCover 各自独立赋值，不能把封面当视频文件 Uri
        var model = new Videos(videoModel.Affiliated, updateVideo.VideoName, updateVideo.VideoCover,
            updateVideo.VideoFileUri,
            updateVideo.BriefIntroduction, updateVideo.Tags.ToList());
        await videoServiceDI.VideoRepository.UpdateByVideoAsync(model);

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
}

/// <summary>视频点赞请求 DTO。</summary>
public record VideoLikeRequest(Guid UserGuid, string Field, bool IsLike = true);
