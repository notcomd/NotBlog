using Microsoft.AspNetCore.Http.HttpResults;
using Video.Domain.Entities;
using Video.Domain.Server;
using Video.Web.API.Dto.Request;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频接口
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

        return group;
    }

    private static async Task<Results<Ok<IVideoResult<List<Videos>>>, JsonHttpResult<IVideoResult<List<Videos>>>>>
        GetByVideoListAsync(VideoService videoService)
    {
        var videoModel = await videoService.GetByVideosAllAsync();
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    private static async Task<Results<Ok<IVideoResult<List<Videos>>>, JsonHttpResult<IVideoResult<List<Videos>>>>>
        GetByVideoPage(int index, int pageSize, VideoService videoService)
    {
        var videoModel = await videoService.PagesByVideosAsync(index, pageSize);
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    private static async Task<Results<Ok<IVideoResult<Videos>>, JsonHttpResult<IVideoResult<Videos>>>>
        GetByVideoNameAsync(string videoName, VideoService videoService)
    {
        var videoModel = await videoService.GetByVideoAsync(videoName);
        return TypedResults.Ok(new IVideoResult<Videos>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    private static async Task<Results<Ok<IVideoResult<List<Videos>>>, JsonHttpResult<IVideoResult<List<Videos>>>>>
        BlurredByVideoAsync(string videoName, VideoService videoService)
    {
        var videoModel = await videoService.BlurredByVideoAsync(videoName);
        return TypedResults.Ok(new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "OK", videoModel));
    }

    private static async Task<IResult> UpdateByVideoAsync(
        RequestUpdateByVideo updateVideo,
        VideoService videoService,
        ILogger<VideoService> logger)
    {
        if (updateVideo is null)
        {
            logger.LogError("updateVideo is null");
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400, "Request body is null.", null),
                statusCode: 400);
        }

        var videoModel = await videoService.GetByVideoAsync(updateVideo.VideoGuid);

        if (videoModel.VideoGuid != updateVideo.AffiliatedUserGuid)
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultUnauthorized, 403,
                    "Unauthorized update attempt.", "Warning: do not do this!"),
                statusCode: 403);

        var model = new Videos(videoModel.Affiliated, updateVideo.VideoName, updateVideo.VideoCover,
            updateVideo.VideoCover,
            updateVideo.BriefIntroduction, updateVideo.Tags);
        await videoService.UpdateByVideoAsync(model);

        return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200, "Update successful.", "UP!"));
    }
}
