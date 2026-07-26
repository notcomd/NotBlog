using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频流接口
/// </summary>
public static class VideoStreamEndpoints
{
    public static RouteGroupBuilder MapVideoStreamEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videostream")
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
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoStreamEndpoint");

        var video = await videoService.GetByVideoAsync(videoGuid);
        if (video is null)
            return Results.NotFound(new { error = "Video not found" });

        if (!video.VideoControl.VideoDisplay || video.VideoControl.VideoDelete)
            return Results.Json(new { error = "Video is not available" }, statusCode: 403);

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

        // Increment watch count (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                video.VideoQuote.UpWatch();
                await videoRepository.UpdateByQuoteAsync(video.VideoQuote);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to increment watch count for video {VideoGuid}", videoGuid);
            }
        });

        return Results.File(responseStream, contentType, enableRangeProcessing: true);
    }
}
