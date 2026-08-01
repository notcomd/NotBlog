using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;
using Video.Web.API.Application.Commands;
using Video.Web.API.Dto.Request;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频接口 — 查询视频列表 + 视频点赞（CQRS + 幂等性）
/// </summary>
public static class VideoEndpoints
{
    public static RouteGroupBuilder MapVideoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/video")
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

        // --- POST 视频点赞 ---
        group.MapPost("/{videoGuid:guid}/like", LikeVideoAsync)
            .WithName("LikeVideo")
            .WithDescription("Like/unlike a video (upvote/down/ballot/share)");

        return group;
    }

    /// <summary>
    /// 获取所有视频
    /// </summary>
    private static async Task<Results<Ok<IVideoResult<List<Videos>>>,
     JsonHttpResult<IVideoResult<List<Videos>>>>>
        GetByVideoListAsync([FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.FindByVideoListAsync();
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    /// <summary>
    /// 根据分页获取视频
    /// </summary>
    private static async Task<Results<Ok<IVideoResult<List<Videos>>>,
    JsonHttpResult<IVideoResult<List<Videos>>>>>
        GetByVideoPage(int index, int pageSize, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.PageByVideoAsync(index, pageSize);
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    /// <summary>
    /// 根据视频名称获取视频
    /// </summary>
    private static async Task<Results<Ok<IVideoResult<Videos>>,
    JsonHttpResult<IVideoResult<Videos>>>>
        GetByVideoNameAsync(string videoName, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.FindByVideoName(videoName);
        return TypedResults.Ok(new IVideoResult<Videos>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    /// <summary>
    /// 模糊搜索视频
    /// </summary>
    private static async Task<Results<Ok<IVideoResult<List<Videos>>>,
    JsonHttpResult<IVideoResult<List<Videos>>>>>
        BlurredByVideoAsync(string videoName, [FromServices] VideoServiceDI videoServiceDI)
    {
        var videoModel = await videoServiceDI.VideoRepository.BlurredByVideoName(videoName);
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    /// <summary>
    /// 更新视频信息
    /// </summary>
    private static async Task<IResult> UpdateByVideoAsync(
        [FromBody] RequestUpdateByVideo updateVideo,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        if (updateVideo is null)
        {
            videoServiceDI.Logger.LogError("updateVideo is null");
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400, "Request body is null.", null),
                statusCode: 400);
        }

        var videoModel = await videoServiceDI.VideoRepository.FindByVideoAsync(updateVideo.VideoGuid);

        if (videoModel.VideoGuid != updateVideo.AffiliatedUserGuid)
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultUnauthorized, 403,
                    "Unauthorized update attempt.", "Warning: do not do this!"),
                statusCode: 403);

        var model = new Videos(videoModel.Affiliated, updateVideo.VideoName, updateVideo.VideoCover,
            updateVideo.VideoCover,
            updateVideo.BriefIntroduction, updateVideo.Tags);
        await videoServiceDI.VideoRepository.UpdateByVideoAsync(model);

        return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200, "Update successful.", "UP!"));
    }

    /// <summary>
    /// 视频点赞/取消点赞（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> LikeVideoAsync(
        Guid videoGuid,
        [FromBody] VideoLikeRequest request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var validFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "upvote", "down", "ballot", "share" };

            if (!validFields.Contains(request.Field))
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        $"Invalid field '{request.Field}'. Valid: upvote, down, ballot, share.", null),
                    statusCode: 400);

            var command = new LikeVideoCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: request.UserGuid,
                Field: request.Field,
                IsLike: request.IsLike);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to like video.", null),
                    statusCode: 400);

            logger.LogInformation("Video {VideoGuid}: {Field} like operation, NewCount={Count}",
                videoGuid, request.Field, result.NewCount);

            return Results.Ok(new IVideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Video like updated.", new { Field = request.Field, NewCount = result.NewCount }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to like video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }
}

/// <summary>视频点赞请求 DTO。</summary>
public record VideoLikeRequest(Guid UserGuid, string Field, bool IsLike = true);
