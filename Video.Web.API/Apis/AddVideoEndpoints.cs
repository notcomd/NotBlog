using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Web.API.Application.Commands;
using Video.Web.API.Dto.Request;
using Video.Domain.ValueObjects;

namespace Video.Web.API.Apis;

/// <summary>
/// 添加视频接口 — 通过 gRPC 调用 FileDev 服务上传视频文件和封面。
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
            .WithDescription("Upload video file and cover image via gRPC to FileDev service")
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

            // 读取视频文件内容
            byte[] videoFileContent;
            await using (var videoStream = videoFile.OpenReadStream())
            {
                using var ms = new MemoryStream();
                await videoStream.CopyToAsync(ms);
                videoFileContent = ms.ToArray();
            }

            // 读取封面图片内容（如果有）
            byte[]? coverImageContent = null;
            string? coverImageFileName = null;
            if (coverImage is { Length: > 0 })
            {
                await using var coverStream = coverImage.OpenReadStream();
                using var ms = new MemoryStream();
                await coverStream.CopyToAsync(ms);
                coverImageContent = ms.ToArray();
                coverImageFileName = coverImage.FileName;
            }

            // 构建通过 gRPC 上传的命令（带幂等性 RequestId）
            var userId = request.AffiliatedAuthorizes.FirstOrDefault();
            var command = new UploadVideoViaGrpcCommand(
                RequestId: Guid.CreateVersion7(),
                UserId: userId,
                VideoName: request.VideoName,
                BriefIntroduction: request.BriefIntroduction,
                VideoFileContent: videoFileContent,
                VideoFileName: videoFile.FileName,
                CoverImageContent: coverImageContent,
                CoverImageFileName: coverImageFileName,
                Tags: request.Tags,
                VideoControl: VideoControl.VideoControlBuilder());

            var result = await videoServiceDI.NotMediator.SendAsync(command);

            if (!result.Success)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultInternalServerError, 500,
                        result.ErrorMessage ?? "Upload failed.", null),
                    statusCode: 500);

            logger.LogInformation("Video created via gRPC: {VideoName} ({VideoGuid})",
                request.VideoName, result.VideoGuid);

            return Results.Ok(new IVideoResult<string>(VideoResultType.VideoResultOk, 200,
                "Video created successfully.", result.VideoGuid.ToString()));
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
