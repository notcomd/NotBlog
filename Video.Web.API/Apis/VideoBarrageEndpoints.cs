using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.ValueObjects;
using Video.Web.API.Application.Commands;
using Video.Web.API.Dto.Request;
using Video.Web.API.Dto.Response;

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
            .WithTags("VideoBarrage");

        group.MapPost("/", AddBarrageAsync)
            .WithName("AddBarrage")
            .WithDescription("Publish a danmaku (barrage) on a video — supports text, image, and mixed");

        group.MapGet("/{videoGuid:guid}", GetBarragesAsync)
            .WithName("GetBarrages")
            .WithDescription("Get barrages for a video");

        return group;
    }

    private static async Task<IResult> AddBarrageAsync(
       [FromForm] RequestAddBarrage request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var hasText = !string.IsNullOrWhiteSpace(request.VideoBarrageBody);
            var hasImages = request.VideoImages is { Count: > 0 };

            if (!hasText && !hasImages)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "弹幕必须包含文本或图片内容", null),
                    statusCode: 400);

            var video = await videoServiceDI.VideoService.GetByVideoAsync(request.VideoGuid);
            if (video is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Video not found.", null),
                    statusCode: 404);

            var domainImages = request.VideoImages?
                .Select(img => new VideoImage(img.ImageUrl, img.SortOrder, img.Description,
                    img.Width, img.Height, img.Format, img.FileSize, img.ThumbnailUrl))
                .ToList();

            var command = new AddVideoBarrageCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: request.VideoGuid,
                UserGuid: request.UserGuid,
                Body: request.VideoBarrageBody,
                VideoImages: domainImages);

            var barrageGuid = await videoServiceDI.NotMediator.SendAsync(command);

            logger.LogInformation("Barrage added to video {VideoGuid} by user {UserGuid}",
                request.VideoGuid, request.UserGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Barrage published successfully.", barrageGuid.ToString()));
        }
        catch (ArgumentException ex)
        {
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400, ex.Message, null),
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add barrage to video {VideoGuid}", request.VideoGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

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
                    new IVideoResult<List<BarrageResponse>>(VideoResultType.VideoResultNotFound, 404,
                        "Video not found.", null),
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
                        b.TimeSpace.CreateAt, b.IsDelete, images);
                })
                .ToList() ?? [];

            return Results.Ok(new IVideoResult<List<BarrageResponse>>(VideoResultType.VideoResultOk, 200,
                "Success.", barrages));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get barrages for video {VideoGuid}", videoGuid);
            return Results.Json(
                new IVideoResult<List<BarrageResponse>>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }
}
