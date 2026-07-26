using Microsoft.AspNetCore.Http.HttpResults;
using NotMediator;
using Video.Domain.Cache;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;
using Video.Web.API.Dto.Request;
using Video.Web.API.Dto.Response;
using Video.Web.API.Application.Commands;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频评论接口
/// </summary>
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

        group.MapPut("/{reviewGuid:guid}/interaction", UpdateReviewInteractionAsync)
            .WithName("UpdateReviewInteraction")
            .WithDescription("Increment or decrement a comment interaction counter (upvote/stars/watch/down/ballot/share)");

        group.MapGet("/{reviewGuid:guid}/interaction", GetReviewInteractionAsync)
            .WithName("GetReviewInteraction")
            .WithDescription("Get interaction counter values for a specific review");

        return group;
    }

    private static async Task<IResult> AddVideoReviewAsync(
        RequestAddReview request,
        IVideoService videoService,
        INotMediator notMediator,
        IVideoRepository videoRepository,
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

            var video = await videoService.GetByVideoAsync(request.VideoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                    statusCode: 404);

            var command=new AddVideoReviewCommand(request.VideoGuid, request.UserGuid, request.RootReview, request.Body, request.VideoImages);
            await notMediator.SendAsync(command);

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

    /// <summary>
    /// 获取视频评论
    /// </summary>
    /// <param name="videoGuid">视频ID</param>
    /// <param name="videoService">视频服务</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>视频评论列表</returns>
    private static async Task<IResult> GetVideoReviewsAsync(
        Guid videoGuid,
        IVideoService videoService,
        IVideoCacheService? cacheService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            List<VideoReview>? reviews = null;
            if (cacheService is not null)
            {
                reviews = await cacheService.GetVideoReviewsAsync(videoGuid);
            }

            if (reviews is null)
            {
                var video = await videoService.GetByVideoAsync(videoGuid);
                if (video is null)
                    return Results.Json(
                        new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                            "Video not found.", null),
                        statusCode: 404);

                var allReviews = video.VideoReviews?.ToList() ?? [];
                reviews = allReviews.Where(r => r.RootReview == null).ToList();

                if (cacheService is not null)
                    _ = cacheService.SetVideoReviewsAsync(videoGuid, reviews);
            }

            var response = reviews
                .Select(r => new VideoReviewResponse(
                    r.VideoReviewGuid, r.VideoGuid, r.UserGuid,
                    r.RootReview, r.VideoReviewBody,
                    r.TimeSpace.CreateAt, r.TimeSpace.UpdateAt,
                    r.VideoQuote.Upvote, r.VideoQuote.Stars, r.VideoQuote.Watch))
                .ToList();

            return Results.Ok(new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", response));
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

    /// <summary>
    /// 获取评论回复
    /// </summary>
    /// <param name="reviewGuid">评论ID</param>
    /// <param name="videoGuid">视频ID</param>
    /// <param name="videoService">视频服务</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>评论回复列表</returns>
    private static async Task<IResult> GetReviewRepliesAsync(
        Guid reviewGuid,
        Guid videoGuid,
        IVideoService videoService,
        IVideoCacheService? cacheService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            List<VideoReview>? replies = null;
            if (cacheService is not null)
            {
                replies = await cacheService.GetVideoReviewRepliesAsync(reviewGuid);
            }

            if (replies is null)
            {
                var video = await videoService.GetByVideoAsync(videoGuid);
                if (video is null)
                    return Results.Json(
                        new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                            "Video not found.", null),
                        statusCode: 404);

                replies = video.VideoReviews?
                    .Where(r => r.RootReview == reviewGuid)
                    .ToList() ?? [];

                if (cacheService is not null)
                    _ = cacheService.SetVideoReviewRepliesAsync(reviewGuid, replies);
            }

            var response = replies
                .Select(r => new VideoReviewResponse(
                    r.VideoReviewGuid, r.VideoGuid, r.UserGuid,
                    r.RootReview, r.VideoReviewBody,
                    r.TimeSpace.CreateAt, r.TimeSpace.UpdateAt,
                    r.VideoQuote.Upvote, r.VideoQuote.Stars, r.VideoQuote.Watch))
                .ToList();

            return Results.Ok(new IVideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", response));
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

    /// <summary>
    /// 更新评论互动
    /// </summary>
    /// <param name="reviewGuid">评论ID</param>
    /// <param name="request">互动互动请求</param>
    /// <param name="videoService">视频服务</param>
    /// <param name="videoRepository">视频仓库</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>更新结果</returns>
    private static async Task<IResult> UpdateReviewInteractionAsync(
        Guid reviewGuid,
        RequestReviewInteraction request,
        INotMediator mediator,
        IVideoService videoService,
        IVideoRepository videoRepository,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            if (!request.IsValid(out var error))
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400, error!, null),
                    statusCode: 400);

            var video = await videoService.GetByVideoAsync(request.VideoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                    statusCode: 404);

            var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == reviewGuid);
            if (review is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Review not found.", null),
                    statusCode: 404);

            var quote = review.VideoQuote;
            var normalized = request.Field.ToLowerInvariant();

            if (request.IsIncrement)
            {
                switch (normalized)
                {
                    case "upvote": quote.UpUpvote(); break;
                    case "stars": quote.UpStars(); break;
                    case "watch": quote.UpWatch(); break;
                    case "down": quote.UpDown(); break;
                    case "ballot": quote.UpBallot(); break;
                    case "share": quote.UpShare(); break;
                    default:
                        return Results.Json(
                            new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                                $"Invalid quote field: {request.Field}", null),
                            statusCode: 400);
                }
            }
            else
            {
                switch (normalized)
                {
                    case "upvote": quote.DownUpvote(); break;
                    case "stars": quote.DownStars(); break;
                    case "down": quote.DownDown(); break;
                    case "ballot": quote.DownBallot(); break;
                    case "share": quote.DownShare(); break;
                    default:
                        return Results.Json(
                            new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                                $"Invalid quote field: {request.Field}", null),
                            statusCode: 400);
                }
            }

            await videoRepository.UpdateByVideoAsync(video);

            var direction = request.IsIncrement ? "incremented" : "decremented";
            logger.LogInformation("Review {ReviewGuid}: {Field} {Direction}",
                reviewGuid, request.Field, direction);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                $"Review interaction '{request.Field}' {direction} successfully.", "OK"));
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Review not found.", null),
                statusCode: 404);
        }
        catch (ArgumentException ex)
        {
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400, ex.Message, null),
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update interaction for review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }


    /// <summary>
    /// 获取评论互动
    /// </summary>
    /// <param name="reviewGuid">评论ID</param>
    /// <param name="videoGuid">视频ID</param>
    /// <param name="videoService">视频服务</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>评论互动</returns>
    private static async Task<IResult> GetReviewInteractionAsync(
        Guid reviewGuid,
        Guid videoGuid,
        IVideoService videoService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<object>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == reviewGuid);
            if (review is null)
                return Results.Json(
                    new IVideoResult<object>(VideoResultType.VideoResultNotFound, 404,
                        "Review not found.", null),
                    statusCode: 404);

            var interaction = new
            {
                review.VideoReviewGuid,
                review.VideoQuote.Upvote,
                review.VideoQuote.Stars,
                review.VideoQuote.Watch,
                review.VideoQuote.Down,
                review.VideoQuote.Ballot,
                review.VideoQuote.Share
            };

            return Results.Ok(new IVideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Success.", interaction));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get interaction for review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new IVideoResult<object>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }
}
