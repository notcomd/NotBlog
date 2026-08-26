
using Google.Protobuf;

using Grpc.Core;


using FileIdentityProto = FileDev.Web.API.Grpc.FileIdentity;
using FileInfoProto = FileDev.Web.API.Grpc.FileInfo;
using FileTypeProto = FileDev.Web.API.Grpc.FileType;

// 本文件位于 FileDev.Web.API.Grpc 命名空间，裸 FileIdentity/FileType/ChunkUploadStatus 会被解析为
// proto 类型；以下别名显式绑定领域枚举，避免与 proto 类型冲突（领域与 proto 成员命名不同）。
using FileIdentityDomain = FileDev.Domain.Enum.FileIdentity;
using FileTypeDomain = FileDev.Domain.Enum.FileType;
using ChunkUploadStatusDomain = FileDev.Domain.Enum.ChunkUploadStatus;

namespace FileDev.Web.API.Grpc;

/// <summary>
/// gRPC 文件存储服务实现：薄适配器。
/// 仅负责「解析调用者 → 构造命令/查询 → 调用应用服务（<see cref="INotMediator"/>）→ 转换响应」，
/// 参数校验、权限/配额检查、存储操作与事务提交全部下沉到应用服务层；
/// 业务异常由 <see cref="GrpcExceptionMapperInterceptor"/> 统一映射为 gRPC 状态码。
/// </summary>
public class FileStorageServiceGRPC(INotMediator mediator, IOptionsSnapshot<NotFileStorageOptions> optionsSnapshot) : FileStorage.FileStorageBase
{


    private readonly INotMediator _mediator =
        mediator ?? throw new ArgumentNullException(nameof(mediator));


    private readonly int StreamChunkSize = optionsSnapshot.Value.ChunkFileSize;
    // ═══════════════════════════════════════════════════
    // 认证辅助（S-08）
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 从 <see cref="ServerCallContext.UserState"/> 获取拦截器解析出的调用者 id。
    /// 所有 gRPC 方法都必须先调用此方法（无 token 时由拦截器直接拒绝）。
    /// </summary>
    private static Guid GetCallerId(ServerCallContext context)
    {
        if (context.UserState.TryGetValue(GrpcJwtAuthInterceptor.CallerUserIdStateKey, out var value)
            && value is Guid uid)
            return uid;

        throw new RpcException(new Status(StatusCode.Unauthenticated, "未认证"));
    }

    // ═══════════════════════════════════════════════════
    // 文件元数据操作
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public override async Task<GetFileInfoResponse> GetFileInfo(
        GetFileInfoRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        var file = await _mediator.SendAsync(
            new GetFileInfoQuery { UserId = callerId, FileId = fileId },
            context.CancellationToken);

        return new GetFileInfoResponse
        {
            Success = true,
            FileInfo = MapToFileInfo(file)
        };
    }

    /// <summary>
    /// 列出用户文件（S-08：仅允许列出调用者自己的文件，忽略客户端传入的 UserId）
    /// </summary>
    public override async Task<ListUserFilesResponse> ListUserFiles(
        ListUserFilesRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var result = await _mediator.SendAsync(
            new ListUserFilesQuery
            {
                UserId = callerId,
                Page = request.Page,
                PageSize = request.PageSize
            },
            context.CancellationToken);

        var response = new ListUserFilesResponse
        {
            Success = true,
            TotalCount = result.TotalCount
        };
        response.Files.AddRange(result.Files.Select(MapToFileInfo));
        return response;
    }

    /// <summary>
    /// 删除文件（软删除 + 物理文件清理由领域事件驱动）
    /// </summary>
    public override async Task<DeleteFileResponse> DeleteFile(
        DeleteFileRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        await _mediator.SendAsync(
            new DeleteFileCommand { UserId = callerId, FileId = fileId },
            context.CancellationToken);

        return new DeleteFileResponse { Success = true };
    }

    /// <summary>
    /// 更新文件信息（S-08：仅文件所有者可更新）
    /// </summary>
    public override async Task<UpdateFileInfoResponse> UpdateFileInfo(
        UpdateFileInfoRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        var updated = await _mediator.SendAsync(
            new UpdateFileInfoCommand
            {
                UserId = callerId,
                FileId = fileId,
                FileName = request.FileName,
                FileTags = request.FileTags?.ToHashSet(),
                FileDescription = request.FileDescription,
                FileIdentity = MapToDomainIdentity(request.FileIdentity),
                FileMd5 = request.FileMd5
            },
            context.CancellationToken);

        return new UpdateFileInfoResponse
        {
            Success = true,
            FileInfo = MapToFileInfo(updated)
        };
    }

    // ═══════════════════════════════════════════════════
    // 小文件上传/下载
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 上传文件
    /// </summary>
    public override async Task<UploadFileResponse> UploadFile(
        UploadFileRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var file = await _mediator.SendAsync(
            new UploadFileCommand
            {
                UserId = callerId,
                FileName = request.FileName,
                FileContent = request.FileContent.ToByteArray(),
                FileTags = request.FileTags?.ToHashSet(),
                FileDescription = request.FileDescription,
                FileIdentity = MapToDomainIdentity(request.FileIdentity),
                ExpectedMd5 = request.ExpectedMd5
            },
            context.CancellationToken);

        return new UploadFileResponse
        {
            Success = true,
            FileId = file.FileId.ToString(),
            FileUri = file.FileUri.ToString(),
            FileMd5 = file.FileMd5,
            FileSize = file.FileSize
        };
    }

    /// <summary>
    /// 下载文件（流式响应）
    /// </summary>
    public override async Task DownloadFile(
        DownloadFileRequest request,
        IServerStreamWriter<DownloadFileResponse> responseStream,
        ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        var result = await _mediator.SendAsync(
            new DownloadFileQuery { UserId = callerId, FileId = fileId },
            context.CancellationToken);

        var contentType = GetContentType(result.File.FileName);
        await using (result.Content)
        {
            var buffer = new byte[StreamChunkSize];
            int read;
            while ((read = await result.Content.ReadAsync(buffer, context.CancellationToken)) > 0)
            {
                await responseStream.WriteAsync(new DownloadFileResponse
                {
                    ChunkData = ByteString.CopyFrom(buffer, 0, read),
                    FileName = result.File.FileName,
                    FileSize = result.File.FileSize,
                    ContentType = contentType
                });
            }
        }
    }

    // ═══════════════════════════════════════════════════
    // 大文件分片上传
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 初始化分片上传
    /// </summary>
    public override async Task<InitChunkUploadResponse> InitChunkUpload(
        InitChunkUploadRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var record = await _mediator.SendAsync(
            new ChunkUploadInitCommand
            {
                UserId = callerId,
                FileName = request.FileName,
                TotalSize = request.TotalSize,
                ChunkSize = StreamChunkSize,
                TotalChunks = request.TotalChunks,
                FileMd5 = request.FileMd5 ?? string.Empty,
                FileType = MapToDomainFileType(request.FileType),
                FileIdentity = MapToDomainIdentity(request.FileIdentity),
                FileTags = request.FileTags?.ToHashSet(),
                FileDescription = request.FileDescription
            },
            context.CancellationToken);

        return new InitChunkUploadResponse
        {
            Success = true,
            FileKey = record.FileKey,
            TotalChunks = record.TotalChunks,
            ChunkSize = record.ChunkSize
        };
    }

    /// <summary>
    /// 上传分片
    /// </summary>
    public override async Task<UploadChunkResponse> UploadChunk(
        UploadChunkRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var result = await _mediator.SendAsync(
            new UploadChunkCommand
            {
                UserId = callerId,
                FileKey = request.FileKey,
                ChunkIndex = request.ChunkIndex,
                ChunkContent = request.ChunkData.ToByteArray(),
                ChunkHash = request.ChunkMd5
            },
            context.CancellationToken);

        return new UploadChunkResponse
        {
            Success = true,
            ChunkIndex = result.ChunkIndex,
            ChunkMd5 = result.ChunkMd5
        };
    }

    /// <summary>
    /// 查询分片状态
    /// </summary>
    public override async Task<GetChunkStatusResponse> GetChunkStatus(
        GetChunkStatusRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var result = await _mediator.SendAsync(
            new ChunkStatusQuery { UserId = callerId, FileKey = request.FileKey },
            context.CancellationToken);

        var response = new GetChunkStatusResponse
        {
            Success = true,
            FileKey = result.FileKey,
            TotalChunks = result.TotalChunks,
            Status = MapToProtoChunkStatus(result.Status)
        };
        response.UploadedChunks.AddRange(result.UploadedChunks);
        return response;
    }

    /// <summary>
    /// 合并分片
    /// </summary>
    public override async Task<MergeChunksResponse> MergeChunks(
        MergeChunksRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var file = await _mediator.SendAsync(
            new MergeChunksCommand
            {
                UserId = callerId,
                FileKey = request.FileKey,
                FileName = request.FileName,
                FileTags = request.FileTags?.ToHashSet(),
                FileDescription = request.FileDescription
            },
            context.CancellationToken);

        return new MergeChunksResponse
        {
            Success = true,
            FileId = file.FileId.ToString(),
            FileUri = file.FileUri.ToString(),
            FileMd5 = file.FileMd5,
            FileSize = file.FileSize
        };
    }

    /// <summary>
    /// 取消分片上传
    /// </summary>
    public override async Task<CancelChunkUploadResponse> CancelChunkUpload(
        CancelChunkUploadRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        await _mediator.SendAsync(
            new CancelChunksCommand { UserId = callerId, FileKey = request.FileKey },
            context.CancellationToken);

        return new CancelChunkUploadResponse { Success = true };
    }

    // ═══════════════════════════════════════════════════
    // 图片专用操作
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 上传图片
    /// </summary>
    public override async Task<UploadImageResponse> UploadImage(
        UploadImageRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);

        var result = await _mediator.SendAsync(
            new UploadImageCommand
            {
                UserId = callerId,
                FileName = request.FileName,
                ImageContent = request.ImageContent.ToByteArray(),
                FileTags = request.FileTags?.ToHashSet(),
                FileDescription = request.FileDescription,
                FileIdentity =MapToDomainIdentity(request.FileIdentity),
                ValidateFormat = request.ValidateFormat
            },
            context.CancellationToken);

        return new UploadImageResponse
        {
            Success = true,
            FileId = result.File.FileId.ToString(),
            FileUri = result.File.FileUri.ToString(),
            FileMd5 = result.File.FileMd5,
            FileSize = result.File.FileSize,
            Width = result.Width,
            Height = result.Height,
            Format = result.Format
        };
    }

    /// <summary>
    /// 下载图片（流式响应）
    /// </summary>
    public override async Task DownloadImage(
        DownloadImageRequest request,
        IServerStreamWriter<DownloadImageResponse> responseStream,
        ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        var result = await _mediator.SendAsync(
            new DownloadFileQuery { UserId = callerId, FileId = fileId },
            context.CancellationToken);

        var contentType = GetContentType(result.File.FileName);
        await using (result.Content)
        {
            var buffer = new byte[StreamChunkSize];
            int read;
            while ((read = await result.Content.ReadAsync(buffer, context.CancellationToken)) > 0)
            {
                await responseStream.WriteAsync(new DownloadImageResponse
                {
                    ChunkData = ByteString.CopyFrom(buffer, 0, read),
                    FileName = result.File.FileName,
                    FileSize = result.File.FileSize,
                    ContentType = contentType
                });
            }
        }
    }

    /// <summary>
    /// 获取图片信息
    /// </summary>
    public override async Task<GetImageInfoResponse> GetImageInfo(
        GetImageInfoRequest request, ServerCallContext context)
    {
        var callerId = GetCallerId(context);
        if (!Guid.TryParse(request.FileId, out var fileId))
            throw new ArgumentException("无效的文件ID");

        var result = await _mediator.SendAsync(
            new GetImageInfoQuery { UserId = callerId, FileId = fileId },
            context.CancellationToken);

        return new GetImageInfoResponse
        {
            Success = true,
            ImageInfo = new ImageInfo
            {
                FileInfo = MapToFileInfo(result.File),
                Width = result.Width,
                Height = result.Height,
                Format = result.Format
            }
        };
    }

    // ═══════════════════════════════════════════════════
    // 私有映射辅助方法
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 将域文件实体映射为gRPC文件信息协议缓冲区
    /// </summary>
    private static FileInfoProto MapToFileInfo(NotFile file)
    {
        return new FileInfoProto
        {
            FileId = file.FileId.ToString(),
            UserId = file.UserId.ToString(),
            FileName = file.FileName,
            FileDescription = file.FileDescription,
            FileSize = file.FileSize,
            FileUri = file.FileUri?.ToString() ?? string.Empty,
            FileMd5 = file.FileMd5,
            FileIdentity = MapToProtoIdentity(file.FileIdentity),
            FileType = MapToProtoFileType(ResolveFileType(
                Path.GetExtension(file.FileName).ToLowerInvariant())),
            UploadTime = file.UploadTime.ToString("O"),
            UpdateTime = file.UpdateTime.ToString("O"),
            IsDeleted = file.IsDeleted
        };
    }

    /// <summary>
    /// 将域文件身份枚举映射为gRPC文件身份协议缓冲区
    /// </summary>
    private static FileIdentityProto MapToProtoIdentity(FileIdentityDomain identity) => identity switch
    {
        FileIdentityDomain.FilePublic => FileIdentityProto.FilePublic,
        FileIdentityDomain.FilePrivate => FileIdentityProto.FilePrivate,
        FileIdentityDomain.FilePrivatePublic => FileIdentityProto.FilePrivatePublic,
        FileIdentityDomain.FilePasswordProtected => FileIdentityProto.FilePasswordProtected,
        _ => FileIdentityProto.FilePrivate
    };

    /// <summary>
    /// 将gRPC文件身份协议缓冲区映射为域文件身份枚举
    /// </summary>
    private static FileIdentityDomain MapToDomainIdentity(FileIdentityProto identity) => identity switch
    {
        FileIdentityProto.FilePublic => FileIdentityDomain.FilePublic,
        FileIdentityProto.FilePrivate => FileIdentityDomain.FilePrivate,
        FileIdentityProto.FilePrivatePublic => FileIdentityDomain.FilePrivatePublic,
        FileIdentityProto.FilePasswordProtected => FileIdentityDomain.FilePasswordProtected,
        _ => FileIdentityDomain.FilePrivate
    };

    /// <summary>
    /// 将域文件类型枚举映射为gRPC文件类型协议缓冲区
    /// </summary>
    private static FileTypeProto MapToProtoFileType(FileTypeDomain fileType) => fileType switch
    {
        FileTypeDomain.FileImage => FileTypeProto.FileImage,
        FileTypeDomain.FileVideo => FileTypeProto.FileVideo,
        FileTypeDomain.FileAudio => FileTypeProto.FileAudio,
        FileTypeDomain.FileFile => FileTypeProto.FileDocument,
        FileTypeDomain.CompressFiles => FileTypeProto.FileCompress,
        _ => FileTypeProto.FileOther
    };

    /// <summary>
    /// 将gRPC文件类型协议缓冲区映射为域文件类型枚举
    /// </summary>
    private static FileTypeDomain MapToDomainFileType(FileTypeProto fileType) => fileType switch
    {
        FileTypeProto.FileImage => FileTypeDomain.FileImage,
        FileTypeProto.FileVideo => FileTypeDomain.FileVideo,
        FileTypeProto.FileAudio => FileTypeDomain.FileAudio,
        FileTypeProto.FileDocument => FileTypeDomain.FileFile,
        FileTypeProto.FileCompress => FileTypeDomain.CompressFiles,
        _ => FileTypeDomain.FileFile
    };

    /// <summary>
    /// 将域文件块上传状态枚举映射为gRPC文件块上传状态协议缓冲区
    /// </summary>
    private static ChunkUploadStatus MapToProtoChunkStatus(
        ChunkUploadStatusDomain status) => status switch
        {
            ChunkUploadStatusDomain.Pending => ChunkUploadStatus.ChunkPending,
            ChunkUploadStatusDomain.Uploading => ChunkUploadStatus.ChunkUploading,
            ChunkUploadStatusDomain.Merged => ChunkUploadStatus.ChunkMerged,
            ChunkUploadStatusDomain.Failed => ChunkUploadStatus.ChunkFailed,
            ChunkUploadStatusDomain.Cancelled => ChunkUploadStatus.ChunkCancelled,
            _ => ChunkUploadStatus.ChunkPending
        };

    private static FileTypeDomain ResolveFileType(string ext) => FileApiHelpers.ResolveFileType(ext);

    /// <summary>
    /// 获取文件内容类型（S-17：.html/.htm/.svg 可被浏览器直接渲染，一律按 application/octet-stream 返回）
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            // S-17：禁止浏览器直接渲染
            ".svg" or ".html" or ".htm" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}
