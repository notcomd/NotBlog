using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Web.API.Application.Commands;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频观看统计接口 — 遵循 CQRS 架构，记录命令带幂等性保护。
/// </summary>
public static class VideoWatchStatsEndpoints
{
    public static RouteGroupBuilder MapVideoWatchStatsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videowatch")
            .WithTags("VideoWatchStats");

        // POST 开始/更新观看进度
        group.MapPost("/{videoGuid:guid}/progress", RecordWatchProgressAsync)
            .WithName("RecordWatchProgress")
            .WithDescription("Record or update video watch progress for a user");

        // POST 结束观看
        group.MapPost("/{videoGuid:guid}/end", EndWatchAsync)
            .WithName("EndWatch")
            .WithDescription("Mark video watching as ended");

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
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            if (request.Progress is < 0 or > 1)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Progress must be between 0 and 1.", null),
                    statusCode: 400);

            var command = new RecordVideoWatchCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: request.UserGuid,
                Progress: request.Progress,
                LastPositionSeconds: request.LastPositionSeconds);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to record watch progress.", null),
                    statusCode: 400);

            return Results.Ok(new IVideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Watch progress recorded.", new { WatchHistoryGuid = result.WatchHistoryGuid }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record watch progress for video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 结束观看（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> EndWatchAsync(
        Guid videoGuid,
        [FromBody] EndWatchRequest request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var command = new EndVideoWatchCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: request.UserGuid);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to end watch.", null),
                    statusCode: 400);

            return Results.Ok(new IVideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Watch ended.", new { WatchHistoryGuid = result.WatchHistoryGuid }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to end watch for video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取视频观看统计（查询操作）
    /// </summary>
    private static async Task<IResult> GetWatchStatsAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var video = await videoServiceDI.VideoRepository.FindByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                    statusCode: 404);

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

            return Results.Ok(new IVideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Success.", stats));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get watch stats for video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }
}

/// <summary>观看进度请求 DTO。</summary>
public record WatchProgressRequest(Guid UserGuid, double Progress, double LastPositionSeconds);

/// <summary>结束观看请求 DTO。</summary>
public record EndWatchRequest(Guid UserGuid);
