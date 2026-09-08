
using Commons.Result;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频弹幕接口 — 支持文本、图片及混合弹幕。
/// 所有写操作通过 CQRS 命令 + Redis 幂等性保护。
/// </summary>
public static class VideoBarrageEndpoints
{
    public static RouteGroupBuilder MapVideoBarrageEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videobarrage")
            .RequireResourcePermissions("api:videobarrage")
            .WithTags("VideoBarrage");

        group.MapPost("/", AddBarrageAsync)
            .WithName("AddBarrage")
            .WithDescription("Publish a danmaku (barrage) on a video — supports text, image, and mixed")
            .RequireAuthorization();

        group.MapGet("/{videoGuid:guid}", GetBarragesAsync)
            .WithName("GetBarrages")
            .WithDescription("Get barrages for a video");

        group.MapDelete("/{videoGuid:guid}/{barrageGuid:guid}", DeleteBarrageAsync)
            .WithName("DeleteBarrage")
            .WithDescription("Delete a specific barrage from a video")
            .RequireAuthorization();

        return group;
    }

    /// <summary>
    /// 添加视频弹幕（命令操作 — CQRS + 幂等性）
    /// </summary>
    /// <param name="request">弹幕添加请求</param>
    /// <param name="videoServiceDI">视频服务依赖</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <returns></returns>
    private static async Task<IResult> AddBarrageAsync(
       [FromForm] RequestAddBarrage request,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            // S-18.2：弹幕归属用户由服务端从 JWT 解析，忽略客户端传入的 UserGuid，防伪造上报
            var callerGuid = currentUser.GetUserId();
            if (callerGuid == Guid.Empty)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Unauthorized. Please login first.", 401),
                    statusCode: 401);

            var hasText = !string.IsNullOrWhiteSpace(request.VideoBarrageBody);
            var hasImages = request.VideoImages is { Count: > 0 };

            if (!hasText && !hasImages)
                return Results.Json(
                    ApiResponseResult<string>.Failure("弹幕必须包含文本或图片内容", 400),
                    statusCode: 400);

            // S-17：弹幕文本长度上限（100 字符），防止超大弹幕拖垮渲染
            if (hasText && request.VideoBarrageBody!.Length > 100)
                return Results.Json(
                    ApiResponseResult<string>.Failure("弹幕文本长度不能超过 100 个字符", 400),
                    statusCode: 400);

            var video = await videoServiceDI.VideoService.GetByVideoAsync(request.VideoGuid);
            if (video is null)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Video not found.", 404),
                    statusCode: 404);

            var domainImages = request.VideoImages?
                .Select(img => new VideoImage(img.ImageUrl, img.SortOrder, img.Description,
                    img.Width, img.Height, img.Format, img.FileSize, img.ThumbnailUrl))
                .ToList();

            // S-17：弹幕文本 HTML 净化（转义 & < > " '），防止存储 XSS 载荷后由前端渲染执行
            var sanitizedBody = hasText ? WebUtility.HtmlEncode(request.VideoBarrageBody) : request.VideoBarrageBody;

            var command = new AddVideoBarrageCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: request.VideoGuid,
                UserGuid: callerGuid,
                Body: sanitizedBody,
                VideoImages: domainImages,
                TimeAt: request.TimeAt);

            var barrageGuid = await videoServiceDI.NotMediator.SendAsync(command);

            logger.LogInformation("Barrage added to video {VideoGuid} by user {UserGuid}",
                request.VideoGuid, callerGuid);

            return Results.Ok(barrageGuid.ToString());
        }
        catch (ArgumentException ex)
        {
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 400),
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add barrage to video {VideoGuid}", request.VideoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取视频弹幕列表（查询操作 — CQRS）
    /// </summary>
    /// <param name="videoGuid">视频 GUID</param>
    /// <param name="videoServiceDI">视频服务依赖</param>
    /// <returns></returns>
    private static async Task<IResult> GetBarragesAsync(
        Guid videoGuid,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var video = await videoServiceDI.VideoService.GetByVideoAsync(videoGuid);
            if (video is null)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Video not found.", 404),
                    statusCode: 404);

            var barrages = video.VideoBarrageList?
                .Where(b => !b.IsDelete)
                .Select(b =>
                {
                    var images = b.VideoImages?.Select(img => new BarrageImageResponse(
                        img.ImageUrl, img.ThumbnailUrl, img.Width, img.Height,
                        img.Format, img.Description, img.SortOrder)).ToList();

                    return new BarrageResponse(
                        b.VideoBarrageGuid, b.VideoGuid, b.UserGuid,
                        b.VideoBarrageBody, b.BarrageType.ToString(),
                        b.TimeAt, b.TimeSpace.CreateAt, b.IsDelete, images);
                })
                .ToList() ?? [];

            return Results.Ok(barrages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get barrages for video {VideoGuid}", videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 删除视频弹幕（命令操作 — CQRS + 幂等性）
    /// </summary>
    /// <param name="videoGuid">视频 GUID</param>
    /// <param name="barrageGuid">弹幕 GUID</param>
    /// <param name="videoServiceDI">视频服务依赖</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <returns></returns>
    private static async Task<IResult> DeleteBarrageAsync(
        Guid videoGuid,
        Guid barrageGuid,
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

            var command = new DeleteVideoBarrageCommand(
                VideoGuid: videoGuid,
                VideoBarrageGuid: barrageGuid
                );

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (result)
                return Results.Json(
                    ApiResponseResult<string>.Failure("Delete failed.", 500),
                    statusCode: 500);

            logger.LogInformation("Barrage {BarrageGuid} deleted from video {VideoGuid} by user {UserGuid}",
                barrageGuid, videoGuid, callerGuid);

            return Results.Ok(barrageGuid.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete barrage {BarrageGuid} from video {VideoGuid}", barrageGuid, videoGuid);
            return Results.Json(
                ApiResponseResult<string>.Failure(ex.Message, 500),
                statusCode: 500);
        }
    }
}
