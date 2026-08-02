using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IServices;
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
            .RequireAuthorization()
            .WithMetadata(new RequestSizeLimitAttribute(500_000_000)); // 500MB max

        return group;
    }

    private static async Task<IResult> AddVideoAsync(
        [FromForm] RequestAddVideo request,
        IFormFile videoFile,
        [FromForm] IFormFile? coverImage,
        [FromServices] VideoServiceDI videoServiceDI,
        [FromServices] ICurrentUserService currentUser)
    {
        var logger = videoServiceDI.Logger;

        try
        {
            if (videoFile is null || videoFile.Length == 0)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        "Video file is required.", null),
                    statusCode: 400);

            // F-08.2：上传类型白名单校验（.mp4/.webm/.mkv/.mov/.avi/.flv），非允许类型直接拒绝
            var allowedExtensions = new[] { ".mp4", ".webm", ".mkv", ".mov", ".avi", ".flv" };
            var fileExtension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 400,
                        $"Unsupported video format '{fileExtension}'. Allowed: mp4, webm, mkv, mov, avi, flv.", null),
                    statusCode: 400);

            // F-08.2：上传大小校验（≤500MB），超限返回 413
            const long maxVideoSize = 500L * 1024 * 1024;
            if (videoFile.Length > maxVideoSize)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultBadRequest, 413,
                        "Video file exceeds the 500MB limit.", null),
                    statusCode: 413);

            // 上传身份由服务端解析当前用户，禁止信任客户端传入的 AffiliatedAuthorizes
            var userId = currentUser.UserGuid;
            if (userId == Guid.Empty)
                return Results.Json(
                    new IVideoResult<string>(VideoResultType.VideoResultUnauthorized, 401,
                        "Unauthorized. Please login first.", null),
                    statusCode: 401);

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
