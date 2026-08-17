using NotMediator;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频评论接口 — 遵循 CQRS 架构，命令操作带幂等性保护。
/// </summary>
public static class VideoReviewEndpoints
{
    /// <summary>
    /// 访问控制（S-07）：私有/定时视频仅作者或被授权者可读取。
    /// </summary>
    private static bool IsAccessForbidden(Videos video, ICurrentUserService currentUser)
    {
        if (video.VideoControl.AuthorVideo == AuthorVideo.VideoPublic)
            return false;

        var callerGuid = currentUser.GetUserId();
        return callerGuid == Guid.Empty || video.Affiliated is null || !video.Affiliated.Contains(callerGuid);
    }

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

        group.MapGet("/{reviewGuid:guid}/interaction", GetReviewInteractionAsync)
            .WithName("GetReviewInteraction")
            .WithDescription("Get interaction counter values for a specific review");

        // 评论点赞接口（通过 CQRS Command）
        group.MapPost("/{reviewGuid:guid}/like", LikeReviewAsync)
            .WithName("LikeReview")
            .WithDescription("Like/unlike a review (upvote/down/ballot/share)");

        return group;
    }

    private static async Task<IResult> AddVideoReviewAsync(
        RequestAddReview request,
        [FromServices] IVideoService videoService,
        [FromServices] INotMediator notMediator,
        [FromServices] IVideoRepository videoRepository,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            if (string.IsNullOrWhiteSpace(request.Body) && (request.VideoImages is null || request.VideoImages.Count == 0))
                return Results.Json(
                    new VideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Comment body or images are required.", null),
                    statusCode: 400);

            if (request.VideoImages is { Count: > 9 })
                return Results.Json(
                    new VideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Maximum 9 images per comment.", null),
                    statusCode: 400);

            var video = await videoService.GetByVideoAsync(request.VideoGuid);
            if (video is null)
                return Results.Json(
                    new VideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                    statusCode: 404);

            var command = new AddVideoReviewCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: request.VideoGuid,
                UserGuid: request.UserGuid,
                RootReview: request.RootReview,
                Body: request.Body,
                VideoImages: request.VideoImages);

            await notMediator.SendAsync(command);

            logger.LogInformation("Review added to video {VideoGuid} by user {UserGuid}",
                request.VideoGuid, request.UserGuid);

            return Results.Ok(new VideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Review added successfully.", "OK"));
        }
        catch (AggregateException)
        {
            return Results.Json(
                new VideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add review to video {VideoGuid}", request.VideoGuid);
            return Results.Json(
                new VideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取视频评论（查询操作）
    /// </summary>
    private static async Task<IResult> GetVideoReviewsAsync(
        Guid videoGuid,
        IVideoService videoService,
        IVideoCacheService? cacheService,
        ILoggerFactory loggerFactory,
        ICurrentUserService currentUser)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            // 访问控制：私有/定时视频仅作者或被授权者可查看评论
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            if (IsAccessForbidden(video, currentUser))
                return Results.Json(
                    new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultUnauthorized, 403,
                        "Video is private or protected.", null),
                    statusCode: 403);

            List<VideoReview>? reviews = null;
            if (cacheService is not null)
            {
                reviews = await cacheService.GetVideoReviewsAsync(videoGuid);
            }

            if (reviews is null)
            {
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

            return Results.Ok(new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get reviews for video {VideoGuid}", videoGuid);
            return Results.Json(
                new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取评论回复（查询操作）
    /// </summary>
    private static async Task<IResult> GetReviewRepliesAsync(
        Guid reviewGuid,
        Guid videoGuid,
        [FromServices] IVideoService videoService,
        [FromServices] IVideoCacheService? cacheService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            // 访问控制：私有/定时视频仅作者或被授权者可查看评论回复
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            if (IsAccessForbidden(video, currentUser))
                return Results.Json(
                    new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultUnauthorized, 403,
                        "Video is private or protected.", null),
                    statusCode: 403);

            List<VideoReview>? replies = null;
            if (cacheService is not null)
            {
                replies = await cacheService.GetVideoReviewRepliesAsync(reviewGuid);
            }

            if (replies is null)
            {
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

            return Results.Ok(new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get replies for review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new VideoResult<List<VideoReviewResponse>>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 评论点赞/取消点赞（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> LikeReviewAsync(
        Guid reviewGuid,
        [FromBody] RequestReviewInteraction request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            if (!request.IsValid(out var error))
                return Results.Json(
                    new VideoResult<string>(VideoResultType.VideoResultBadRequest, 400, error!, null),
                    statusCode: 400);

            var command = new QuoteVideoReviewCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: request.VideoGuid,
                UserGuid: Guid.Empty, // 可从认证上下文获取
                ReviewGuid: reviewGuid,
                Field: request.Field,
                IsLike: request.IsIncrement);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new VideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to like review.", null),
                    statusCode: 400);

            logger.LogInformation("Review {ReviewGuid}: {Field} like operation completed, NewCount={Count}",
                reviewGuid, request.Field, result.NewCount);

            return Results.Ok(new VideoResult<object>(VideoResultType.VideoResultOk, 200,
                $"Review like '{request.Field}' updated.", new { NewCount = result.NewCount }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to like review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new VideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取评论互动统计（查询操作）
    /// </summary>
    private static async Task<IResult> GetReviewInteractionAsync(
        Guid reviewGuid,
        Guid videoGuid,
        [FromServices] IVideoService videoService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = loggerFactory.CreateLogger("VideoReviewEndpoint");

        try
        {
            var video = await videoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    new VideoResult<object>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
                    statusCode: 404);

            if (IsAccessForbidden(video, currentUser))
                return Results.Json(
                    new VideoResult<object>(VideoResultType.VideoResultUnauthorized, 403,
                        "Video is private or protected.", null),
                    statusCode: 403);

            var review = video.VideoReviews?.FirstOrDefault(r => r.VideoReviewGuid == reviewGuid);
            if (review is null)
                return Results.Json(
                    new VideoResult<object>(VideoResultType.VideoResultNotFound, 404,
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

            return Results.Ok(new VideoResult<object>(VideoResultType.VideoResultOk, 200,
                "Success.", interaction));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get interaction for review {ReviewGuid}", reviewGuid);
            return Results.Json(
                new VideoResult<object>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }
}
