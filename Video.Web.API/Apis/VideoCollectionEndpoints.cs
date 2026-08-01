using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Web.API.Application.Commands;

namespace Video.Web.API.Apis;

/// <summary>
/// 视频收藏接口 — 遵循 CQRS 架构，命令操作带幂等性保护。
/// </summary>
public static class VideoCollectionEndpoints
{
    public static RouteGroupBuilder MapVideoCollectionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videocollection")
            .WithTags("VideoCollection");

        // GET 查询所有收藏
        group.MapGet("/", GetByVideoCollectionAsync)
            .WithName("GetVideoCollectionList")
            .WithDescription("Get all video collections");

        // GET 查询单个收藏夹
        group.MapGet("/{collectionGuid:guid}", GetCollectionByIdAsync)
            .WithName("GetCollectionById")
            .WithDescription("Get a specific video collection by ID");

        // POST 添加视频到收藏夹
        group.MapPost("/{collectionGuid:guid}/videos", AddVideoToCollectionAsync)
            .WithName("AddVideoToCollection")
            .WithDescription("Add a video to a collection");

        // DELETE 从收藏夹移除视频
        group.MapDelete("/{collectionGuid:guid}/videos/{videoGuid:guid}", RemoveVideoFromCollectionAsync)
            .WithName("RemoveVideoFromCollection")
            .WithDescription("Remove a video from a collection");

        return group;
    }

    private static async Task<IResult> GetByVideoCollectionAsync(
        [FromServices] VideoServiceDI videoServiceDI)
    {
        try
        {
            var collections = await videoServiceDI.VideoCollectionRepository
                .FindByVideoCollectionListAsync();
            return Results.Ok(new IVideoResult<List<VideoCollection>>(
                VideoResultType.VideoResultOk, 200, "Success.", collections));
        }
        catch (Exception ex)
        {
            videoServiceDI.Logger.LogError(ex, "Failed to get video collections");
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetCollectionByIdAsync(
        Guid collectionGuid,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        try
        {
            var collection = await videoServiceDI.VideoCollectionRepository
                .FindByVideoCollectionAsync(collectionGuid);
            if (collection is null)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultNotFound, 404, "Collection not found.", null),
                    statusCode: 404);

            return Results.Ok(new IVideoResult<VideoCollection>(
                VideoResultType.VideoResultOk, 200, "Success.", collection));
        }
        catch (Exception ex)
        {
            videoServiceDI.Logger.LogError(ex, "Failed to get collection {CollectionGuid}", collectionGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 添加视频到收藏夹（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> AddVideoToCollectionAsync(
        Guid collectionGuid,
        [FromBody] AddToCollectionRequest request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            if (request.VideoGuid == Guid.Empty || collectionGuid == Guid.Empty)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "VideoGuid and CollectionGuid are required.", null),
                    statusCode: 400);

            var command = new AddVideoToCollectionCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: request.VideoGuid,
                UserGuid: request.UserGuid,
                CollectionGuid: collectionGuid);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to add video to collection.", null),
                    statusCode: 400);

            logger.LogInformation("Video {VideoGuid} added to collection {CollectionGuid}",
                request.VideoGuid, collectionGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Video added to collection.", "OK"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add video {VideoGuid} to collection {CollectionGuid}",
                request.VideoGuid, collectionGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }

    /// <summary>
    /// 从收藏夹移除视频（命令操作 — CQRS + 幂等性）
    /// </summary>
    private static async Task<IResult> RemoveVideoFromCollectionAsync(
        Guid collectionGuid,
        Guid videoGuid,
        [FromBody] RemoveFromCollectionRequest request,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            var command = new RemoveVideoFromCollectionCommand(
                RequestId: Guid.CreateVersion7(),
                VideoGuid: videoGuid,
                UserGuid: request.UserGuid,
                CollectionGuid: collectionGuid);

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        result.ErrorMessage ?? "Failed to remove video from collection.", null),
                    statusCode: 400);

            logger.LogInformation("Video {VideoGuid} removed from collection {CollectionGuid}",
                videoGuid, collectionGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Video removed from collection.", "OK"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove video {VideoGuid} from collection {CollectionGuid}",
                videoGuid, collectionGuid);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500, ex.Message, null),
                statusCode: 500);
        }
    }
}

/// <summary>添加到收藏夹请求 DTO。</summary>
public record AddToCollectionRequest(Guid VideoGuid, Guid UserGuid);

/// <summary>从收藏夹移除请求 DTO。</summary>
public record RemoveFromCollectionRequest(Guid UserGuid);
