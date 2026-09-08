
using Commons.Result;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频观看统计接口 — 遵循 CQRS 架构，记录命令带幂等性保护。
/// </summary>
public static class VideoWatchStatsEndpoints
{
    public static RouteGroupBuilder MapVideoWatchStatsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videowatch")
            .RequireResourcePermissions("api:videowatch")
            .WithTags("VideoWatchStats");

        // POST 开始/更新观看进度（S-18：仅登录用户，UserGuid 由服务端解析，不信任客户端）
        group.MapPost("/{videoGuid:guid}/progress", RecordWatchProgressAsync)
            .WithName("RecordWatchProgress")
            .WithDescription("Record or update video watch progress for a user")
            .RequireAuthorization();

        // POST 结束观看（S-18：仅登录用户，UserGuid 由服务端解析，不信任客户端）
        group.MapPost("/{videoGuid:guid}/end", EndWatchAsync)
            .WithName("EndWatch")
            .WithDescription("Mark video watching as ended")
            .RequireAuthorization();

        // GET 视频观看统计
        group.MapGet("/{videoGuid:guid}/stats", GetWatchStatsAsync)
            .WithName("GetWatchStats")
            .WithDescription("Get watch statistics for a video");

        return group;
    }

    /// <summary>
    /// 记录/更新观看进度（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> RecordWatchProgressAsync(
        Guid videoGuid,
        [FromBody] WatchProgressRequest request,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            // S-18.2：观看历史归属的用户由服务端从 JWT 解析，忽略客户端传入的 UserGuid，防伪造上报
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            if (request.Progress is < 0 or > 1)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Progress must be between 0 and 1.", 400),
                    statusCode: 400);

            var command = new RecordVideoWatchCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: callerGuid,
                Progress: request.Progress,
                LastPositionSeconds: request.LastPositionSeconds);

            var result = await videoServiceDI. NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    ApiResponseResult<string>.Failure(result.ErrorMessage ?? "Failed to record watch progress.", 400),
                    statusCode: 400);

            return Results.Ok(new { WatchHistoryGuid = result.WatchHistoryGuid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record watch progress for video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 结束观看（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> EndWatchAsync(
        Guid videoGuid,
        [FromBody] EndWatchRequest request,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            // S-18.2：用户由服务端从 JWT 解析，忽略客户端传入的 UserGuid
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            var command = new EndVideoWatchCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: callerGuid);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    ApiResponseResult<string>.Failure(result.ErrorMessage ?? "Failed to end watch.", 400),
                    statusCode: 400);

            return Results.Ok(new { WatchHistoryGuid = result.WatchHistoryGuid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to end watch for video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取视频观看统计（查询操作）
    /// </summary>
    private static async Task<IResult> GetWatchStatsAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var video = await videoServiceDI.VideoRepository.FindByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Video not found.", 404),
                    statusCode: 404);

            // 访问控制（S-07）：私有/定时视频仅作者或被授权者可查看统计
            if (video.VideoControl.AuthorVideo != AuthorVideo.VideoPublic)
            {
                var callerGuid = currentUser.GetUserId();
                if (callerGuid == Guid.Empty || video.Affiliated is null || !video.Affiliated.Contains(callerGuid))
                    return Results.Json(
                        ApiResponseResult<string>.Failure("Video is private or protected.", 403),
                        statusCode: 403);
            }

            var stats = new
            {
                VideoGuid = videoGuid,
                video.VideoQuote.Watch,
                video.VideoQuote.Upvote,
                video.VideoQuote.Down,
                video.VideoQuote.Ballot,
                video.VideoQuote.Share,
                video.VideoQuote.Stars
            };

            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get watch stats for video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }
}

/// <summary>观看进度请求 DTO。</summary>
public record WatchProgressRequest(Guid UserGuid, double Progress, double LastPositionSeconds);

/// <summary>结束观看请求 DTO。</summary>
public record EndWatchRequest(Guid UserGuid);
