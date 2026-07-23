using Video.Domain.Entities;
using Video.Domain.Server;
using Video.Web.API.Dto.Request;
using Video.Web.API.Dto.Response;

namespace Video.Web.API.Apis;

public static class VideoReviewEndpoints
{
    public static RouteGroupBuilder MapVideoReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videoreview")
            .WithTags("VideoReview");

        group.MapPost("/", AddVideoReviewAsync)
            .WithName("AddVideoReview")
            .WithDescription("Add a comment/review to a video");

        group.MapGet("/{videoGuid:guid}", GetVideoReviewsAsync)
            .WithName("GetVideoReviews")
            .WithDescription("Get reviews for a video (top-level only)");

        group.MapGet("/replies/{reviewGuid:guid}", GetReviewRepliesAsync)
            .WithName("GetReviewReplies")
            .WithDescription("Get replies to a specific review");

        return group;
    }

    private static async Task<IResult> AddVideoReviewAsync(
        RequestAddReview request,
        VideoService videoService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            if (string.IsNullOrWhiteSpace(request.Body) && (request.VideoImages is null || request.VideoImages.Count == 0))
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Comment body or images are required.", null),
                    statusCode: 400);

            if (request.VideoImages is { Count: > 9 })
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Maximum 9 images per comment.", null),
                    statusCode: 400);

            await videoService.AddByVideoReviewAsync(
                request.VideoGuid, request.UserGuid, request.RootReview,
                request.Body, request.VideoImages);

            logger.LogInformation("Review added to video {VideoGuid} by user {UserGuid}",
                request.VideoGuid, request.UserGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Review added successfully.", "OK"));
        }
        catch (AggregateException)
        {
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add review to video {VideoGuid}", request.VideoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetVideoReviewsAsync(
        Guid videoGuid,
        VideoService videoService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            var reviews = video.VideoReviews?
                .Where(r => r.RootReview == null)
                .Select(r => new VideoReviewResponse(
                    r.VideoReviewGuid, r.VideoGuid, r.UserGuid,
                    r.RootReview, r.VideoReviewBody,
                    r.TimeSpace.CreateAt, r.TimeSpace.UpdateAt,
                    r.VideoQuote.Upvote, r.VideoQuote.Stars, r.VideoQuote.Watch))
                .ToList() ?? [];

            return Results.Ok(new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", reviews));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reviews for video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetReviewRepliesAsync(
        Guid reviewGuid,
        Guid videoGuid,
        VideoService videoService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            var replies = video.VideoReviews?
                .Where(r => r.RootReview == reviewGuid)
                .Select(r => new VideoReviewResponse(
                    r.VideoReviewGuid, r.VideoGuid, r.UserGuid,
                    r.RootReview, r.VideoReviewBody,
                    r.TimeSpace.CreateAt, r.TimeSpace.UpdateAt,
                    r.VideoQuote.Upvote, r.VideoQuote.Stars, r.VideoQuote.Watch))
                .ToList() ?? [];

            return Results.Ok(new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", replies));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get replies for review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }
}
