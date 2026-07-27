using FileDev.Domain.Dto.Request;
using FileDev.Domain.Entities;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using FileDev.Infrastructure.Service;
using FileDev.Web.API.Grpc;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;
using FileInfoProto = FileDev.Web.API.Grpc.FileInfo;
using FileIdentityProto = FileDev.Web.API.Grpc.FileIdentity;
using FileTypeProto = FileDev.Web.API.Grpc.FileType;
using DomainFileIdentity = FileDev.Domain.Entities.FileIdentity;
using DomainFileType = FileDev.Domain.Entities.FileType;

namespace FileDev.Web.API.Grpc;

/// <summary>
/// gRPC 文件存储服务实现，提供文件和图片的上传、下载、分片上传、元数据管理等完整功能。
/// 基于现有的领域服务和基础设施层，通过 gRPC 对外暴露文件操作能力。
/// </summary>
public class FileStorageServiceGRPC : FileStorage.FileStorageBase
{
    private readonly INotFileService _notFileService;
    private readonly INotFileStorageService _storageService;
    private readonly IFileChunkManager _chunkManager;
    private readonly IOptionsSnapshot<NotFileStorageOptions> _options;
    private readonly ILogger<FileStorageServiceGRPC> _logger;

    public FileStorageServiceGRPC(
        INotFileService notFileService,
        INotFileStorageService storageService,
        IFileChunkManager chunkManager,
        IOptionsSnapshot<NotFileStorageOptions> options,
        ILogger<FileStorageServiceGRPC> logger)
    {
        _notFileService = notFileService;
        _storageService = storageService;
        _chunkManager = chunkManager;
        _options = options;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // 文件元数据操作
    // ═══════════════════════════════════════════════════

    public override async Task<GetFileInfoResponse> GetFileInfo(
        GetFileInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new GetFileInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
                return new GetFileInfoResponse { Success = false, ErrorMessage = "文件不存在" };

            return new GetFileInfoResponse
            {
                Success = true,
                FileInfo = MapToFileInfo(file)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息失败 FileId={FileId}", request.FileId);
            return new GetFileInfoResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<ListUserFilesResponse> ListUserFiles(
        ListUserFilesRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new ListUserFilesResponse { Success = false, ErrorMessage = "无效的用户ID" };

            var files = await _notFileService.GetFilesByUserIdAsync(userId);
            var fileList = files.Where(f => !f.IsDeleted).ToList();
            var totalCount = fileList.Count;

            var page = request.Page > 0 ? request.Page : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 20;
            var pagedFiles = fileList.Skip((page - 1) * pageSize).Take(pageSize);

            var response = new ListUserFilesResponse
            {
                Success = true,
                TotalCount = totalCount
            };
            response.Files.AddRange(pagedFiles.Select(MapToFileInfo));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出用户文件失败 UserId={UserId}", request.UserId);
            return new ListUserFilesResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<DeleteFileResponse> DeleteFile(
        DeleteFileRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new DeleteFileResponse { Success = false, ErrorMessage = "无效的文件ID" };
            if (!Guid.TryParse(request.UserId, out var userId))
                return new DeleteFileResponse { Success = false, ErrorMessage = "无效的用户ID" };

            await _notFileService.DeleteFileAsync(fileId, userId);
            return new DeleteFileResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件失败 FileId={FileId}", request.FileId);
            return new DeleteFileResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<UpdateFileInfoResponse> UpdateFileInfo(
        UpdateFileInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new UpdateFileInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            var identity = MapToDomainIdentity(request.FileIdentity);
            var tags = request.FileTags?.ToHashSet();

            await _notFileService.UpdateFileAsync(
                fileId,
                request.FileName,
                tags,
                request.FileDescription,
                identity,
                request.FileMd5);

            var updated = await _notFileService.GetFileByIdAsync(fileId);
            return new UpdateFileInfoResponse
            {
                Success = true,
                FileInfo = updated is not null ? MapToFileInfo(updated) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新文件信息失败 FileId={FileId}", request.FileId);
            return new UpdateFileInfoResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ═══════════════════════════════════════════════════
    // 小文件上传/下载
    // ═══════════════════════════════════════════════════

    public override async Task<UploadFileResponse> UploadFile(
        UploadFileRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new UploadFileResponse { Success = false, ErrorMessage = "无效的用户ID" };

            var content = request.FileContent.ToByteArray();
            if (content.Length == 0)
                return new UploadFileResponse { Success = false, ErrorMessage = "文件内容不能为空" };

            if (content.Length > _options.Value.MaxFileSize)
                return new UploadFileResponse
                {
                    Success = false,
                    ErrorMessage = $"文件大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB"
                };

            // 扩展名白名单校验
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && !_options.Value.AllowedExtensions.Contains(ext))
                return new UploadFileResponse { Success = false, ErrorMessage = $"不支持的文件类型: {ext}" };

            // MD5 校验
            if (!string.IsNullOrEmpty(request.ExpectedMd5))
            {
                var actualMd5 = HashHelper.ComputeHash(content, AlgorithmType.MD5);
                if (!string.Equals(actualMd5, request.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                    return new UploadFileResponse
                    {
                        Success = false,
                        ErrorMessage = $"MD5校验失败，预期: {request.ExpectedMd5}，实际: {actualMd5}"
                    };
            }

            var fileGuid = Guid.CreateVersion7();
            var relativePath = $"{userId:N}/{fileGuid}{ext}";

            var storageRequest = new NotFileStorageRequest
            {
                FileRelativePath = relativePath,
                FileContent = content,
                Overwrite = false,
                ExpectedHash = request.ExpectedMd5
            };

            var storageResult = await _storageService.SaveAsync(storageRequest);
            if (!storageResult.Success)
                return new UploadFileResponse { Success = false, ErrorMessage = storageResult.ErrorMessage };

            var fileUri = new Uri($"/files/{relativePath}", UriKind.Relative);
            var tags = request.FileTags?.ToHashSet();
            var fileType = ResolveFileType(ext);
            var identity = MapToDomainIdentity(request.FileIdentity);

            await _notFileService.CreateFileAsync(
                userId, request.FileName, tags, request.FileDescription ?? string.Empty,
                fileType, content.Length, fileUri,
                storageResult.ActualHash ?? string.Empty, identity);

            return new UploadFileResponse
            {
                Success = true,
                FileId = fileGuid.ToString(),
                FileUri = fileUri.ToString(),
                FileMd5 = storageResult.ActualHash ?? string.Empty,
                FileSize = content.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件失败 FileName={FileName}", request.FileName);
            return new UploadFileResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task DownloadFile(
        DownloadFileRequest request,
        IServerStreamWriter<DownloadFileResponse> responseStream,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
            {
                _logger.LogWarning("下载文件失败：无效的文件ID {FileId}", request.FileId);
                return;
            }

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
            {
                _logger.LogWarning("下载文件失败：文件不存在 {FileId}", request.FileId);
                return;
            }

            // 权限校验：私有文件仅允许所有者下载
            if (file.FileIdentity == DomainFileIdentity.FilePrivate
                && !string.Equals(request.UserId, file.UserId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("下载文件失败：无权限访问私有文件 {FileId}", request.FileId);
                return;
            }

            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");
            var (content, storageResponse) = await _storageService.GetContentAsync(relativePath);

            if (!storageResponse.Success || content is null)
            {
                _logger.LogError("下载文件失败：存储读取错误 {FilePath}", relativePath);
                return;
            }

            const int chunkSize = 64 * 1024; // 64KB per chunk
            var contentType = GetContentType(file.FileName);

            for (int offset = 0; offset < content.Length; offset += chunkSize)
            {
                var remaining = Math.Min(chunkSize, content.Length - offset);
                var chunk = new byte[remaining];
                Array.Copy(content, offset, chunk, 0, remaining);

                await responseStream.WriteAsync(new DownloadFileResponse
                {
                    ChunkData = ByteString.CopyFrom(chunk),
                    FileName = file.FileName,
                    FileSize = content.Length,
                    ContentType = contentType
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载文件失败 FileId={FileId}", request.FileId);
        }
    }

    // ═══════════════════════════════════════════════════
    // 大文件分片上传
    // ═══════════════════════════════════════════════════

    public override async Task<InitChunkUploadResponse> InitChunkUpload(
        InitChunkUploadRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new InitChunkUploadResponse { Success = false, ErrorMessage = "无效的用户ID" };

            if (request.TotalSize > _options.Value.MaxFileSize)
                return new InitChunkUploadResponse
                {
                    Success = false,
                    ErrorMessage = $"文件大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB"
                };

            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && !_options.Value.AllowedExtensions.Contains(ext))
                return new InitChunkUploadResponse { Success = false, ErrorMessage = $"不支持的文件类型: {ext}" };

            int chunkSize = (int)_options.Value.ChunkFileSize;
            int totalChunks = request.TotalChunks > 0
                ? request.TotalChunks
                : (int)Math.Ceiling((double)request.TotalSize / chunkSize);

            var fileKey = $"{userId:N}/{Guid.CreateVersion7():N}{ext}";

            var record = await _chunkManager.InitializeUploadAsync(
                fileKey, userId, request.FileName, request.TotalSize,
                chunkSize, totalChunks, request.FileMd5 ?? string.Empty,
                MapToDomainFileType(request.FileType),
                MapToDomainIdentity(request.FileIdentity),
                request.FileTags?.ToHashSet(),
                request.FileDescription,
                context.CancellationToken);

            return new InitChunkUploadResponse
            {
                Success = true,
                FileKey = fileKey,
                TotalChunks = totalChunks,
                ChunkSize = chunkSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化分片上传失败 FileName={FileName}", request.FileName);
            return new InitChunkUploadResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<UploadChunkResponse> UploadChunk(
        UploadChunkRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new UploadChunkResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            var chunkData = request.ChunkData.ToByteArray();
            if (chunkData.Length == 0)
                return new UploadChunkResponse { Success = false, ErrorMessage = "分片数据不能为空" };

            // 检查分片大小
            if (chunkData.Length > _options.Value.ChunkFileSize * 2)
                return new UploadChunkResponse { Success = false, ErrorMessage = "分片数据超出大小限制" };

            var storageResult = await _storageService.UploadChunkAsync(
                request.FileKey, request.ChunkIndex, chunkData, request.ChunkMd5);

            if (!storageResult.Success)
                return new UploadChunkResponse
                {
                    Success = false,
                    ChunkIndex = request.ChunkIndex,
                    ErrorMessage = storageResult.ErrorMessage
                };

            await _chunkManager.MarkChunkUploadedAsync(
                request.FileKey, request.ChunkIndex, context.CancellationToken);

            return new UploadChunkResponse
            {
                Success = true,
                ChunkIndex = request.ChunkIndex,
                ChunkMd5 = storageResult.ActualHash
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex}",
                request.FileKey, request.ChunkIndex);
            return new UploadChunkResponse
            {
                Success = false,
                ChunkIndex = request.ChunkIndex,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetChunkStatusResponse> GetChunkStatus(
        GetChunkStatusRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new GetChunkStatusResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            var record = await _chunkManager.GetUploadStatusAsync(
                request.FileKey, context.CancellationToken);

            if (record is null)
                return new GetChunkStatusResponse { Success = false, ErrorMessage = "未找到分片上传记录" };

            var uploadedChunks = await _chunkManager.GetUploadedChunksAsync(
                request.FileKey, context.CancellationToken);

            var response = new GetChunkStatusResponse
            {
                Success = true,
                FileKey = record.FileKey,
                TotalChunks = record.TotalChunks,
                Status = MapToProtoChunkStatus(record.Status)
            };
            response.UploadedChunks.AddRange(uploadedChunks);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询分片状态失败 FileKey={FileKey}", request.FileKey);
            return new GetChunkStatusResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<MergeChunksResponse> MergeChunks(
        MergeChunksRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new MergeChunksResponse { Success = false, ErrorMessage = "文件Key不能为空" };
            if (!Guid.TryParse(request.UserId, out var userId))
                return new MergeChunksResponse { Success = false, ErrorMessage = "无效的用户ID" };

            // 检查所有分片是否已上传
            var allUploaded = await _chunkManager.AreAllChunksUploadedAsync(
                request.FileKey, context.CancellationToken);
            if (!allUploaded)
                return new MergeChunksResponse { Success = false, ErrorMessage = "还有分片未上传完成" };

            var record = await _chunkManager.GetUploadStatusAsync(
                request.FileKey, context.CancellationToken);
            if (record is null)
                return new MergeChunksResponse { Success = false, ErrorMessage = "未找到分片上传记录" };

            // 合并分片
            var mergeResult = await _storageService.MergeChunksAsync(
                request.FileKey, record.TotalChunks, record.FileMd5);

            if (!mergeResult.Success)
                return new MergeChunksResponse { Success = false, ErrorMessage = mergeResult.ErrorMessage };

            // 标记合并完成
            await _chunkManager.MarkMergedAsync(request.FileKey, context.CancellationToken);

            // 创建文件元数据记录
            var fileUri = new Uri($"/files/{request.FileKey}", UriKind.Relative);
            var tags = request.FileTags?.ToHashSet();

            await _notFileService.CreateFileAsync(
                userId,
                request.FileName ?? record.FileName,
                tags,
                request.FileDescription ?? string.Empty,
                record.FileType,
                mergeResult.FileSize,
                fileUri,
                mergeResult.ActualHash ?? string.Empty,
                record.FileIdentity);

            return new MergeChunksResponse
            {
                Success = true,
                FileUri = fileUri.ToString(),
                FileMd5 = mergeResult.ActualHash ?? string.Empty,
                FileSize = mergeResult.FileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并分片失败 FileKey={FileKey}", request.FileKey);
            return new MergeChunksResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<CancelChunkUploadResponse> CancelChunkUpload(
        CancelChunkUploadRequest request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new CancelChunkUploadResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            await _chunkManager.CancelUploadAsync(request.FileKey, context.CancellationToken);
            return new CancelChunkUploadResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消分片上传失败 FileKey={FileKey}", request.FileKey);
            return new CancelChunkUploadResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ═══════════════════════════════════════════════════
    // 图片专用操作
    // ═══════════════════════════════════════════════════

    public override async Task<UploadImageResponse> UploadImage(
        UploadImageRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
                return new UploadImageResponse { Success = false, ErrorMessage = "无效的用户ID" };

            var imageData = request.ImageContent.ToByteArray();
            if (imageData.Length == 0)
                return new UploadImageResponse { Success = false, ErrorMessage = "图片内容不能为空" };

            // 图片格式验证
            if (request.ValidateFormat)
            {
                var formatResult = ImageValidator.Validate(imageData);
                if (!formatResult.IsValid)
                    return new UploadImageResponse
                    {
                        Success = false,
                        ErrorMessage = $"图片格式验证失败: {formatResult.ErrorMessage}"
                    };

                var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext))
                {
                    var expectedExt = formatResult.Format switch
                    {
                        "jpeg" => ".jpg",
                        _ => $".{formatResult.Format}"
                    };
                    if (ext != expectedExt && ext != $".{formatResult.Format}")
                        return new UploadImageResponse
                        {
                            Success = false,
                            ErrorMessage = $"图片实际格式({formatResult.Format})与扩展名({ext})不匹配"
                        };
                }
            }

            if (imageData.Length > _options.Value.MaxFileSize)
                return new UploadImageResponse
                {
                    Success = false,
                    ErrorMessage = $"图片大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB"
                };

            var ext2 = Path.GetExtension(request.FileName).ToLowerInvariant();
            var fileGuid = Guid.CreateVersion7();
            var relativePath = $"{userId:N}/{fileGuid}{ext2}";

            var storageRequest = new NotFileStorageRequest
            {
                FileRelativePath = relativePath,
                FileContent = imageData,
                Overwrite = false
            };

            var storageResult = await _storageService.SaveAsync(storageRequest);
            if (!storageResult.Success)
                return new UploadImageResponse { Success = false, ErrorMessage = storageResult.ErrorMessage };

            var fileUri = new Uri($"/files/{relativePath}", UriKind.Relative);
            var tags = request.FileTags?.ToHashSet();

            await _notFileService.CreateFileAsync(
                userId, request.FileName, tags, request.FileDescription ?? string.Empty,
                DomainFileType.FileImage, imageData.Length, fileUri,
                storageResult.ActualHash ?? string.Empty,
                MapToDomainIdentity(request.FileIdentity));

            // 获取图片尺寸
            var (width, height) = ImageValidator.GetDimensions(imageData);

            return new UploadImageResponse
            {
                Success = true,
                FileId = fileGuid.ToString(),
                FileUri = fileUri.ToString(),
                FileMd5 = storageResult.ActualHash ?? string.Empty,
                FileSize = imageData.Length,
                Width = width,
                Height = height,
                Format = ext2.TrimStart('.')
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传图片失败 FileName={FileName}", request.FileName);
            return new UploadImageResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task DownloadImage(
        DownloadImageRequest request,
        IServerStreamWriter<DownloadImageResponse> responseStream,
        ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
            {
                _logger.LogWarning("下载图片失败：无效的文件ID {FileId}", request.FileId);
                return;
            }

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
            {
                _logger.LogWarning("下载图片失败：文件不存在 {FileId}", request.FileId);
                return;
            }

            if (file.FileIdentity == DomainFileIdentity.FilePrivate
                && !string.Equals(request.UserId, file.UserId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("下载图片失败：无权限访问私有文件 {FileId}", request.FileId);
                return;
            }

            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");
            var (content, storageResponse) = await _storageService.GetContentAsync(relativePath);

            if (!storageResponse.Success || content is null)
            {
                _logger.LogError("下载图片失败：存储读取错误 {FilePath}", relativePath);
                return;
            }

            var (width, height) = ImageValidator.GetDimensions(content);
            const int chunkSize = 64 * 1024;
            var contentType = GetImageContentType(file.FileName);

            for (int offset = 0; offset < content.Length; offset += chunkSize)
            {
                var remaining = Math.Min(chunkSize, content.Length - offset);
                var chunk = new byte[remaining];
                Array.Copy(content, offset, chunk, 0, remaining);

                await responseStream.WriteAsync(new DownloadImageResponse
                {
                    ChunkData = ByteString.CopyFrom(chunk),
                    FileName = file.FileName,
                    FileSize = content.Length,
                    ContentType = contentType,
                    Width = width,
                    Height = height
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载图片失败 FileId={FileId}", request.FileId);
        }
    }

    public override async Task<GetImageInfoResponse> GetImageInfo(
        GetImageInfoRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new GetImageInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
                return new GetImageInfoResponse { Success = false, ErrorMessage = "文件不存在" };

            // 获取图片内容以解析尺寸
            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");
            var (content, storageResponse) = await _storageService.GetContentAsync(relativePath);

            int width = 0, height = 0;
            string format = "unknown";

            if (storageResponse.Success && content is not null)
            {
                (width, height) = ImageValidator.GetDimensions(content);
                format = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
            }

            return new GetImageInfoResponse
            {
                Success = true,
                ImageInfo = new ImageInfo
                {
                    FileInfo = MapToFileInfo(file),
                    Width = width,
                    Height = height,
                    Format = format
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取图片信息失败 FileId={FileId}", request.FileId);
            return new GetImageInfoResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ═══════════════════════════════════════════════════
    // 私有辅助方法
    // ═══════════════════════════════════════════════════

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

    private static FileIdentityProto MapToProtoIdentity(DomainFileIdentity identity) => identity switch
    {
        DomainFileIdentity.FilePublic => FileIdentityProto.FilePublic,
        DomainFileIdentity.FilePrivate => FileIdentityProto.FilePrivate,
        DomainFileIdentity.FilePrivatePublic => FileIdentityProto.FilePrivatePublic,
        DomainFileIdentity.FilePasswordProtected => FileIdentityProto.FilePasswordProtected,
        _ => FileIdentityProto.FilePrivate
    };

    private static DomainFileIdentity MapToDomainIdentity(FileIdentityProto identity) => identity switch
    {
        FileIdentityProto.FilePublic => DomainFileIdentity.FilePublic,
        FileIdentityProto.FilePrivate => DomainFileIdentity.FilePrivate,
        FileIdentityProto.FilePrivatePublic => DomainFileIdentity.FilePrivatePublic,
        FileIdentityProto.FilePasswordProtected => DomainFileIdentity.FilePasswordProtected,
        _ => DomainFileIdentity.FilePrivate
    };

    private static FileTypeProto MapToProtoFileType(DomainFileType fileType) => fileType switch
    {
        DomainFileType.FileImage => FileTypeProto.FileImage,
        DomainFileType.FileVideo => FileTypeProto.FileVideo,
        DomainFileType.FileAudio => FileTypeProto.FileAudio,
        DomainFileType.CompressFiles => FileTypeProto.FileCompress,
        DomainFileType.FileFile => FileTypeProto.FileOther,
        _ => FileTypeProto.FileOther,
    };

    private static DomainFileType MapToDomainFileType(FileTypeProto fileType) => fileType switch
    {
        FileTypeProto.FileImage => DomainFileType.FileImage,
        FileTypeProto.FileVideo => DomainFileType.FileVideo,
        FileTypeProto.FileAudio => DomainFileType.FileAudio,
        FileTypeProto.FileCompress => DomainFileType.CompressFiles,
        FileTypeProto.FileOther => DomainFileType.FileFile,
        _ => DomainFileType.FileFile,
    };

    private static ChunkUploadStatus MapToProtoChunkStatus(
        Domain.Entities.ChunkUploadStatus status) => status switch
    {
        Domain.Entities.ChunkUploadStatus.Pending => ChunkUploadStatus.ChunkPending,
        Domain.Entities.ChunkUploadStatus.Uploading => ChunkUploadStatus.ChunkUploading,
        Domain.Entities.ChunkUploadStatus.Merged => ChunkUploadStatus.ChunkMerged,
        Domain.Entities.ChunkUploadStatus.Failed => ChunkUploadStatus.ChunkFailed,
        Domain.Entities.ChunkUploadStatus.Cancelled => ChunkUploadStatus.ChunkCancelled,
        _ => ChunkUploadStatus.ChunkPending
    };

    private static DomainFileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico"
            => DomainFileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm"
            => DomainFileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a"
            => DomainFileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2"
            => DomainFileType.CompressFiles,
        _ => DomainFileType.FileFile
    };

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
            ".svg" => "image/svg+xml",
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
            ".html" or ".htm" => "text/html",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    private static string GetImageContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }
}
