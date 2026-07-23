namespace Message.Web.API.APIs;

public static class FilesApi
{
    public static RouteGroupBuilder MapFilesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files")
            .WithTags("Files");

        // Create logger once for all handlers
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("FilesApi");

        // 1. POST / — 上传文件
        group.MapPost("/", async (
            IFileProvider fileProvider,
            [FromBody] UploadFileRequest request) =>
        {
            try
            {
                var attachment = await fileProvider.UploadFileAsync(
                    request.MessageId,
                    request.FileName,
                    request.FileType,
                    request.FileSize,
                    new Uri(request.FileUrl));

                return Results.Ok(ApiResponse<FileAttachmentDto>.Created(MapToDto(attachment), "文件上传成功"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "文件上传失败");
                return Results.Ok(ApiResponse<FileAttachmentDto>.Error("文件上传失败"));
            }
        })
        .WithSummary("上传文件")
        .WithDescription("上传文件附件")
        .Produces<ApiResponse<FileAttachmentDto>>()
        .Accepts<UploadFileRequest>("application/json");

        // 2. GET /{id} — 获取文件
        group.MapGet("/{id}", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var file = await fileProvider.GetFileAsync(id);
                if (file == null)
                    return Results.Json(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"), statusCode: 404);

                return Results.Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(file)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取文件失败");
                return Results.Ok(ApiResponse<FileAttachmentDto>.Error("获取文件失败"));
            }
        })
        .WithSummary("获取文件")
        .WithDescription("根据文件ID获取文件信息")
        .Produces<ApiResponse<FileAttachmentDto>>();

        // 3. GET /{id}/download — 下载文件
        group.MapGet("/{id}/download", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var file = await fileProvider.GetFileAsync(id);
                if (file == null)
                    return Results.Json(ApiResponse.NotFound("文件不存在"), statusCode: 404);

                await fileProvider.RecordDownloadAsync(id);

                return Results.Redirect(file.FileUri.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "下载文件失败");
                return Results.Ok(ApiResponse.Error("下载文件失败"));
            }
        })
        .WithSummary("下载文件")
        .WithDescription("下载文件并记录下载次数，重定向至文件URL")
        .Produces(StatusCodes.Status302Found);

        // 4. GET /{id}/preview — 预览文件
        group.MapGet("/{id}/preview", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var file = await fileProvider.GetFileAsync(id);
                if (file == null)
                    return Results.Json(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"), statusCode: 404);

                if (!fileProvider.IsImage(file.FileType))
                    return Results.Json(ApiResponse<FileAttachmentDto>.BadRequest("该文件不支持预览"), statusCode: 400);

                return Results.Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(file)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "预览文件失败");
                return Results.Ok(ApiResponse<FileAttachmentDto>.Error("预览文件失败"));
            }
        })
        .WithSummary("预览文件")
        .WithDescription("获取文件预览信息，仅支持图片类型")
        .Produces<ApiResponse<FileAttachmentDto>>();

        // 5. DELETE /{id} — 删除文件
        group.MapDelete("/{id}", async (
            Guid id,
            IFileProvider fileProvider,
            ICurrentUserService currentUserService) =>
        {
            try
            {
                await fileProvider.DeleteFileAsync(id);
                return Results.Ok(ApiResponse.Ok("文件已删除"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "删除文件失败");
                return Results.Ok(ApiResponse.Error("删除文件失败"));
            }
        })
        .WithSummary("删除文件")
        .WithDescription("删除指定文件")
        .Produces<ApiResponse>();

        // 6. GET /message/{messageId} — 获取消息附件
        group.MapGet("/message/{messageId}", async (
            Guid messageId,
            IFileProvider fileProvider) =>
        {
            try
            {
                var files = await fileProvider.GetMessageFilesAsync(messageId);
                return Results.Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Ok(files.Select(MapToDto)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取消息附件失败");
                return Results.Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Error("获取消息附件失败"));
            }
        })
        .WithSummary("获取消息附件")
        .WithDescription("根据消息ID获取所有附件文件")
        .Produces<ApiResponse<IEnumerable<FileAttachmentDto>>>();

        // 7. GET /{id}/exists — 检查文件是否存在
        group.MapGet("/{id}/exists", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var exists = await fileProvider.FileExistsAsync(id);
                return Results.Ok(ApiResponse<bool>.Ok(exists));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "检查文件是否存在失败");
                return Results.Ok(ApiResponse<bool>.Error("检查文件是否存在失败"));
            }
        })
        .WithSummary("检查文件是否存在")
        .WithDescription("检查指定文件是否存在")
        .Produces<ApiResponse<bool>>();

        // 8. GET /{id}/download-count — 获取下载次数
        group.MapGet("/{id}/download-count", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var count = await fileProvider.GetDownloadCountAsync(id);
                return Results.Ok(ApiResponse<int>.Ok(count));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取下载次数失败");
                return Results.Ok(ApiResponse<int>.Error("获取下载次数失败"));
            }
        })
        .WithSummary("获取下载次数")
        .WithDescription("获取指定文件的下载次数")
        .Produces<ApiResponse<int>>();

        // 9. GET /{id}/size — 获取文件大小
        group.MapGet("/{id}/size", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var file = await fileProvider.GetFileAsync(id);
                if (file == null)
                    return Results.Json(ApiResponse<string>.NotFound("文件不存在"), statusCode: 404);

                var formattedSize = fileProvider.GetFormattedFileSize(file.FileSize);
                return Results.Ok(ApiResponse<string>.Ok(formattedSize));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取文件大小失败");
                return Results.Ok(ApiResponse<string>.Error("获取文件大小失败"));
            }
        })
        .WithSummary("获取文件大小")
        .WithDescription("获取指定文件的格式化大小")
        .Produces<ApiResponse<string>>();

        // 10. GET /{id}/type — 获取文件类型信息
        group.MapGet("/{id}/type", async (
            Guid id,
            IFileProvider fileProvider) =>
        {
            try
            {
                var file = await fileProvider.GetFileAsync(id);
                if (file == null)
                    return Results.Json(ApiResponse<FileTypeInfo>.NotFound("文件不存在"), statusCode: 404);

                var typeInfo = new FileTypeInfo
                {
                    FileType = file.FileType,
                    IsImage = fileProvider.IsImage(file.FileType),
                    IsVideo = fileProvider.IsVideo(file.FileType),
                    IsAudio = fileProvider.IsAudio(file.FileType),
                    IsDocument = fileProvider.IsDocument(file.FileType)
                };

                return Results.Ok(ApiResponse<FileTypeInfo>.Ok(typeInfo));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "获取文件类型失败");
                return Results.Ok(ApiResponse<FileTypeInfo>.Error("获取文件类型失败"));
            }
        })
        .WithSummary("获取文件类型信息")
        .WithDescription("获取指定文件的类型分类信息（图片/视频/音频/文档）")
        .Produces<ApiResponse<FileTypeInfo>>();

        return group;
    }

    private static FileAttachmentDto MapToDto(FileAttachment file) => new()
    {
        AttachmentId = file.AttachmentId,
        FileName = file.FileName,
        FileType = file.FileType,
        FileSize = file.FileSize,
        FileUrl = file.FileUri.ToString(),
        ThumbnailUrl = file.ThumbnailUri?.ToString(),
        UploadTime = file.UploadTime,
        DownloadCount = file.DownloadCount
    };

    public class FileTypeInfo
    {
        public string FileType { get; init; } = string.Empty;
        public bool IsImage { get; init; }
        public bool IsVideo { get; init; }
        public bool IsAudio { get; init; }
        public bool IsDocument { get; init; }
    }
}
