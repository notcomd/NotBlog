
namespace Video.Web.API.Apis;

/// <summary>
/// 视频流接口
/// </summary>
public static class VideoStreamEndpoints
{
    public static RouteGroupBuilder MapVideoStreamEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videostream")
            .RequireResourcePermissions("api:videostream")
            .WithTags("VideoStream");

        group.MapGet("/{videoGuid:guid}", StreamVideoAsync)
            .WithName("StreamVideo")
            .WithDescription("Stream video file with HTTP Range support for seeking");

        return group;
    }

    private static async Task<IResult> StreamVideoAsync(
        Guid videoGuid,
        HttpRequest httpRequest,
        HttpResponse httpResponse,
        IVideoService videoService,
        IVideoRepository videoRepository,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ICurrentUserService currentUser,
        IRedisCacheService redis)
    {
        var logger = loggerFactory.CreateLogger("VideoStreamEndpoint");

        var video = await videoService.GetByVideoAsync(videoGuid);
        if (video is null)
            return Results.NotFound(new { error = "Video not found" });

        if (!video.VideoControl.VideoDisplay || video.VideoControl.VideoDelete)
            return Results.Json(new { error = "Video is not available" }, statusCode: 403);

        // 访问控制（S-07）：私有/定时视频仅作者或被授权者可访问
        if (video.VideoControl.AuthorVideo != AuthorVideo.VideoPublic)
        {
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty || video.Affiliated is null || !video.Affiliated.Contains(callerGuid))
                return Results.Json(new { error = "Video is private or protected" }, statusCode: 403);
        }

        var fileUri = video.VideoFileUri.ToString();

        var client = httpClientFactory.CreateClient("FileDevProxy");
        var request = new HttpRequestMessage(HttpMethod.Get, fileUri);

        // Forward the Range header for seeking support
        if (httpRequest.Headers.TryGetValue("Range", out var rangeHeader))
            request.Headers.TryAddWithoutValidation("Range", rangeHeader.ToString());

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            return Results.Json(new { error = "Failed to stream video" },
                statusCode: (int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
        httpResponse.Headers["Accept-Ranges"] = "bytes";

        if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
        {
            httpResponse.StatusCode = 206;
            if (response.Content.Headers.TryGetValues("Content-Range", out var contentRange))
                httpResponse.Headers["Content-Range"] = contentRange.FirstOrDefault();
        }

        var responseStream = await response.Content.ReadAsStreamAsync();

        // S-18.1/S-18.2：观看计数改为请求内同步执行（移除 Task.Run，避免请求作用域 DbContext
        // 被释放后抛 ObjectDisposedException），并以「视频+用户+5 分钟窗口」SETNX 去重，
        // 连续 Range 请求 5 分钟内只计 1 次观看；异常不阻断流响应。
        try
        {
            var dedupKey = VideoCacheKeys.VideoWatchWindow(video.VideoGuid, currentUser.GetUserId());
            var shouldCount = await redis.StringSetIfNotExistsAsync(
                dedupKey, "1", VideoCacheKeys.VideoWatchWindowTtl);
            if (shouldCount)
            {
                video.VideoQuote.UpWatch();
                await videoRepository.UpdateByQuoteAsync(video.VideoGuid, video.VideoQuote);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to increment watch count for video {VideoGuid}", videoGuid);
        }

        return Results.File(responseStream, contentType, enableRangeProcessing: true);
    }
}
