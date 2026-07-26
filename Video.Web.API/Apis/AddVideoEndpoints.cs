using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Web.API.Dto.Request;

namespace Video.Web.API.Apis;

/// <summary>
/// 添加视频接口
/// </summary>
public static class AddVideoEndpoints
{
    public static RouteGroupBuilder MapAddVideoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/addvideo")
            .WithTags("AddVideo")
            .DisableAntiforgery();

        group.MapPost("/", AddVideoAsync)
            .WithName("AddVideo")
            .WithDescription("Upload video file and create video metadata")
            .Produces<IVideoResult<string>>(200)
            .ProducesProblem(400)
            .ProducesProblem(500)
            .WithMetadata(new RequestSizeLimitAttribute(500_000_000)); // 500MB max

        return group;
    }

    private static async Task<IResult> AddVideoAsync(
        [FromForm] RequestAddVideo request,
        IFormFile videoFile,
        [FromForm] IFormFile? coverImage,
        [FromServices] VideoServiceDI videoServiceDI)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            if (videoFile is null || videoFile.Length == 0)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Video file is required.", null),
                    statusCode: 400);

            // 1. Upload video file to FileDev service
            await using var videoStream = videoFile.OpenReadStream();
            var uploadResult = await videoServiceDI.FileDevClient.UploadVideoAsync(
                videoStream, videoFile.FileName, request.AffiliatedAuthorizes.FirstOrDefault());

            var coverUri = new Uri(uploadResult.FileUri, UriKind.RelativeOrAbsolute);
            if (coverImage is { Length: > 0 })
            {
                await using var coverStream = coverImage.OpenReadStream();
                var coverResult = await videoServiceDI.FileDevClient.UploadVideoAsync(
                    coverStream, coverImage.FileName, request.AffiliatedAuthorizes.FirstOrDefault());
                coverUri = new Uri(coverResult.FileUri, UriKind.RelativeOrAbsolute);
            }

            // 2. Create video entity and persist
            var videoFileUri = new Uri(uploadResult.FileUri, UriKind.RelativeOrAbsolute);
            var video = new Videos(
                request.AffiliatedAuthorizes,
                request.VideoName,
                coverUri,
                videoFileUri,
                request.BriefIntroduction,
                request.Tags);

            await videoServiceDI.VideoRepository.AddByVideoAsync(video);

            logger.LogInformation("Video created: {VideoName} ({VideoGuid})", request.VideoName, video.VideoGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Video created successfully.", video.VideoGuid.ToString()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create video: {VideoName}", request.VideoName);
            return Results.Json(
                new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500,
                    ex.Message, null),
                statusCode: 500);
        }
    }
}
