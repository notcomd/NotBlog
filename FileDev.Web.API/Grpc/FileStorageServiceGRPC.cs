
using Google.Protobuf;
using Grpc.Core;

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
    private readonly FileDev.Domain.IRepository.INotFileRepository _notFileRepository;
    private readonly ILogger<FileStorageServiceGRPC> _logger;

    public FileStorageServiceGRPC(INotFileService notFileService,
                                  INotFileStorageService storageService,
                                  IFileChunkManager chunkManager,
                                  IOptionsSnapshot<NotFileStorageOptions> options,
                                  FileDev.Domain.IRepository.INotFileRepository notFileRepository,
                                  ILogger<FileStorageServiceGRPC> logger)
    {
        _notFileService = notFileService;
        _storageService = storageService;
        _chunkManager = chunkManager;
        _options = options;
        _notFileRepository = notFileRepository;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // 认证与授权辅助（S-08）
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

    /// <summary>
    /// 校验私有文件归属：非公开文件仅允许所有者访问（S-08）。
    /// </summary>
    private static bool CanAccessFile(NotFile file, Guid callerId)
        => file.FileIdentity != DomainFileIdentity.FilePrivate || file.UserId == callerId;

    /// <summary>
    /// 配额检查（S-09）：已用空间 + 本次新增不得超出 <see cref="NotFileStorageOptions.UserStorageQuota"/>。
    /// </summary>
    private async Task EnsureQuotaAvailableAsync(Guid userId, long additionalBytes, CancellationToken ct)
    {
        var used = await _notFileRepository.GetTotalFileSizeByUserIdAsync(userId);
        if (used + additionalBytes > _options.Value.UserStorageQuota)
            throw new RpcException(new Status(StatusCode.ResourceExhausted, "用户存储配额不足"));
    }

    // ═══════════════════════════════════════════════════
    // 文件元数据操作
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="request">包含文件ID的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含文件信息的响应</returns>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="InvalidOperationException">如果文件已被删除</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<GetFileInfoResponse> GetFileInfo(
        GetFileInfoRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new GetFileInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
                return new GetFileInfoResponse { Success = false, ErrorMessage = "文件不存在" };

            // S-08：私有文件仅所有者可查看元数据
            if (!CanAccessFile(file, callerId))
                return new GetFileInfoResponse { Success = false, ErrorMessage = "无权访问此文件" };

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

    /// <summary>
    /// 列出用户文件
    /// </summary>
    /// <param name="request">包含用户ID的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含用户文件列表的响应</returns>
    /// <exception cref="ArgumentException">如果用户ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<ListUserFilesResponse> ListUserFiles(
        ListUserFilesRequest request, ServerCallContext context)
    {
        try
        {
            // S-08：仅允许列出调用者自己的文件，忽略客户端传入的 UserId
            var userId = GetCallerId(context);

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

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="request">包含文件ID和用户ID的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含删除结果的响应</returns>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="InvalidOperationException">如果文件已被删除</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<DeleteFileResponse> DeleteFile(
        DeleteFileRequest request, ServerCallContext context)
    {
        try
        {
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new DeleteFileResponse { Success = false, ErrorMessage = "无效的文件ID" };

            // S-08：使用服务端解析的调用者 id，忽略客户端传入的 UserId
            var userId = GetCallerId(context);

            await _notFileService.DeleteFileAsync(fileId, userId);
            // F-09.4：软删除需显式持久化并触发 DeleteFileEvent（物理文件清理）
            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);
            return new DeleteFileResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件失败 FileId={FileId}", request.FileId);
            return new DeleteFileResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 更新文件信息
    /// </summary>
    /// <param name="request">包含文件ID和文件信息的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含更新结果的响应</returns>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="InvalidOperationException">如果文件已被删除</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<UpdateFileInfoResponse> UpdateFileInfo(
        UpdateFileInfoRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new UpdateFileInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            // S-08：仅文件所有者可更新元数据
            var existing = await _notFileService.GetFileByIdAsync(fileId);
            if (existing is null || existing.IsDeleted)
                return new UpdateFileInfoResponse { Success = false, ErrorMessage = "文件不存在" };
            if (existing.UserId != callerId)
                return new UpdateFileInfoResponse { Success = false, ErrorMessage = "无权修改此文件" };

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
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="request">包含用户ID、文件名、文件内容和预期MD5的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含上传结果的响应</returns>
    /// <exception cref="ArgumentException">如果用户ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<UploadFileResponse> UploadFile(
        UploadFileRequest request, ServerCallContext context)
    {
        try
        {
            // S-08：使用服务端解析的调用者 id，忽略客户端传入的 UserId
            var userId = GetCallerId(context);

            var content = request.FileContent.ToByteArray();
            if (content.Length == 0)
                return new UploadFileResponse { Success = false, ErrorMessage = "文件内容不能为空" };

            if (content.Length > _options.Value.MaxFileSize)
                return new UploadFileResponse
                {
                    Success = false,
                    ErrorMessage = $"文件大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB"
                };

            // S-09：写入前配额检查
            await EnsureQuotaAvailableAsync(userId, content.Length, context.CancellationToken);

            // 扩展名白名单校验
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && !_options.Value.AllowedExtensions.Contains(ext))
                return new UploadFileResponse { Success = false, ErrorMessage = $"不支持的文件类型: {ext}" };

            // SHA256 校验（F-09.5：上传/查重统一使用 SHA256）
            if (!string.IsNullOrEmpty(request.ExpectedMd5))
            {
                var actualHash = HashHelper.ComputeHash(content);
                if (!string.Equals(actualHash, request.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                    return new UploadFileResponse
                    {
                        Success = false,
                        ErrorMessage = $"哈希校验失败，预期: {request.ExpectedMd5}，实际: {actualHash}"
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

            // 提交元数据：gRPC 服务不经过 Mediator 事务管道，需显式持久化
            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);

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

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="request">包含文件ID和用户ID的请求</param>
    /// <param name="responseStream">gRPC 响应流</param>
    /// <param name="context">gRPC 上下文</param>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="InvalidOperationException">如果文件已被删除</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task DownloadFile(
        DownloadFileRequest request,
        IServerStreamWriter<DownloadFileResponse> responseStream,
        ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);

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

            // S-08：权限校验改用服务端解析的调用者 id，私有文件仅允许所有者下载
            if (!CanAccessFile(file, callerId))
            {
                _logger.LogWarning("下载文件失败：无权限访问私有文件 {FileId}", request.FileId);
                return;
            }

            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");
            // S-09：流式读取，避免整文件读入内存
            var (stream, storageResponse) = await _storageService.GetContentStreamAsync(relativePath);

            if (!storageResponse.Success || stream is null)
            {
                _logger.LogError("下载文件失败：存储读取错误 {FilePath}", relativePath);
                return;
            }

            const int chunkSize = 64 * 1024; // 64KB per chunk
            var contentType = GetContentType(file.FileName);

            await using (stream)
            {
                var buffer = new byte[chunkSize];
                int read;
                while ((read = await stream.ReadAsync(buffer, context.CancellationToken)) > 0)
                {
                    await responseStream.WriteAsync(new DownloadFileResponse
                    {
                        ChunkData = ByteString.CopyFrom(buffer, 0, read),
                        FileName = file.FileName,
                        FileSize = file.FileSize,
                        ContentType = contentType
                    });
                }
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
    /// <summary>
    /// 初始化分片上传
    /// </summary>
    /// <param name="request">包含用户ID、文件名、总大小、总分片数、文件MD5和文件类型的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含初始化结果的响应</returns>
    /// <exception cref="ArgumentException">如果用户ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<InitChunkUploadResponse> InitChunkUpload(
        InitChunkUploadRequest request, ServerCallContext context)
    {
        try
        {
            // S-08：使用服务端解析的调用者 id，忽略客户端传入的 UserId
            var userId = GetCallerId(context);

            if (request.TotalSize > _options.Value.MaxFileSize)
                return new InitChunkUploadResponse
                {
                    Success = false,
                    ErrorMessage = $"文件大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB"
                };

            // S-09：写入前配额检查
            await EnsureQuotaAvailableAsync(userId, request.TotalSize, context.CancellationToken);

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
    /// <summary>
    /// 上传分片
    /// </summary>
    /// <param name="request">包含文件Key、分片索引、分片数据和分片MD5的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含上传结果的响应</returns>
    /// <exception cref="ArgumentException">如果文件Key无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<UploadChunkResponse> UploadChunk(
        UploadChunkRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new UploadChunkResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            // S-08：分片归属校验前置——仅上传任务所有者可上传分片
            var uploadRecord = await _chunkManager.GetUploadStatusAsync(request.FileKey, context.CancellationToken);
            if (uploadRecord is null)
                return new UploadChunkResponse { Success = false, ErrorMessage = "未找到分片上传记录" };
            if (uploadRecord.UserId != callerId)
                return new UploadChunkResponse { Success = false, ErrorMessage = "无权操作此上传任务" };
            if (uploadRecord.Status == Domain.Entities.ChunkUploadStatus.Merged
                || uploadRecord.Status == Domain.Entities.ChunkUploadStatus.Cancelled)
                return new UploadChunkResponse { Success = false, ErrorMessage = "上传任务已结束，无法继续上传" };

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
    /// <summary>
    /// 查询分片状态
    /// </summary>
    /// <param name="request">包含文件Key的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含分片状态的响应</returns>
    /// <exception cref="ArgumentException">如果文件Key无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<GetChunkStatusResponse> GetChunkStatus(
        GetChunkStatusRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new GetChunkStatusResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            var record = await _chunkManager.GetUploadStatusAsync(
                request.FileKey, context.CancellationToken);

            if (record is null)
                return new GetChunkStatusResponse { Success = false, ErrorMessage = "未找到分片上传记录" };

            // S-08：仅上传任务所有者可查询状态
            if (record.UserId != callerId)
                return new GetChunkStatusResponse { Success = false, ErrorMessage = "无权操作此上传任务" };

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
    /// <summary>
    /// 合并分片
    /// </summary>
    /// <param name="request">包含文件Key和用户ID的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含合并结果的响应</returns>
    /// <exception cref="ArgumentException">如果文件Key无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<MergeChunksResponse> MergeChunks(
        MergeChunksRequest request, ServerCallContext context)
    {
        try
        {
            // S-08：使用服务端解析的调用者 id，忽略客户端传入的 UserId
            var userId = GetCallerId(context);
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new MergeChunksResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            var record = await _chunkManager.GetUploadStatusAsync(
                request.FileKey, context.CancellationToken);
            if (record is null)
                return new MergeChunksResponse { Success = false, ErrorMessage = "未找到分片上传记录" };

            // S-08：合并前校验归属，用户 B 无法合并用户 A 的分片
            if (record.UserId != userId)
                return new MergeChunksResponse { Success = false, ErrorMessage = "无权合并此上传任务" };

            // S-09：合并前配额检查
            await EnsureQuotaAvailableAsync(userId, record.TotalSize, context.CancellationToken);

            // 检查所有分片是否已上传
            var allUploaded = await _chunkManager.AreAllChunksUploadedAsync(
                request.FileKey, context.CancellationToken);
            if (!allUploaded)
                return new MergeChunksResponse { Success = false, ErrorMessage = "还有分片未上传完成" };

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

            // 提交元数据：gRPC 服务不经过 Mediator 事务管道，需显式持久化
            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);

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

    /// <summary>
    /// 取消分片上传
    /// </summary>
    /// <param name="request">包含文件Key的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含取消结果的响应</returns>
    /// <exception cref="ArgumentException">如果文件Key无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<CancelChunkUploadResponse> CancelChunkUpload(
        CancelChunkUploadRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (string.IsNullOrWhiteSpace(request.FileKey))
                return new CancelChunkUploadResponse { Success = false, ErrorMessage = "文件Key不能为空" };

            // S-08：仅上传任务所有者可取消
            var record = await _chunkManager.GetUploadStatusAsync(request.FileKey, context.CancellationToken);
            if (record is null)
                return new CancelChunkUploadResponse { Success = false, ErrorMessage = "未找到分片上传记录" };
            if (record.UserId != callerId)
                return new CancelChunkUploadResponse { Success = false, ErrorMessage = "无权操作此上传任务" };

            await _chunkManager.CancelUploadAsync(request.FileKey, context.CancellationToken);
            // S-09：取消时清理临时分片文件
            await _storageService.CleanupChunksAsync(request.FileKey);
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
    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="request">包含用户ID、图片内容、文件名、文件描述和文件类型（可选）的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含上传结果的响应</returns>
    /// <exception cref="ArgumentException">如果用户ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<UploadImageResponse> UploadImage(
        UploadImageRequest request, ServerCallContext context)
    {
        try
        {
            // S-08：使用服务端解析的调用者 id，忽略客户端传入的 UserId
            var userId = GetCallerId(context);

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

            // S-09：写入前配额检查
            await EnsureQuotaAvailableAsync(userId, imageData.Length, context.CancellationToken);

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

            // 提交元数据：gRPC 服务不经过 Mediator 事务管道，需显式持久化
            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);

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

    /// <summary>
    /// 下载图片
    /// </summary>
    /// <param name="request">包含文件ID和用户ID的请求</param>
    /// <param name="responseStream">gRPC 响应流</param>
    /// <param name="context">gRPC 上下文</param>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task DownloadImage(
        DownloadImageRequest request,
        IServerStreamWriter<DownloadImageResponse> responseStream,
        ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);

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

            // S-08：权限校验改用服务端解析的调用者 id
            if (!CanAccessFile(file, callerId))
            {
                _logger.LogWarning("下载图片失败：无权限访问私有文件 {FileId}", request.FileId);
                return;
            }

            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");
            // S-09：流式读取，避免整文件读入内存
            var (stream, storageResponse) = await _storageService.GetContentStreamAsync(relativePath);

            if (!storageResponse.Success || stream is null)
            {
                _logger.LogError("下载图片失败：存储读取错误 {FilePath}", relativePath);
                return;
            }

            const int chunkSize = 64 * 1024;
            var contentType = GetImageContentType(file.FileName);

            await using (stream)
            {
                var buffer = new byte[chunkSize];
                int read;
                while ((read = await stream.ReadAsync(buffer, context.CancellationToken)) > 0)
                {
                    await responseStream.WriteAsync(new DownloadImageResponse
                    {
                        ChunkData = ByteString.CopyFrom(buffer, 0, read),
                        FileName = file.FileName,
                        FileSize = file.FileSize,
                        ContentType = contentType
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载图片失败 FileId={FileId}", request.FileId);
        }
    }

    /// <summary>
    /// 获取图片信息
    /// </summary>
    /// <param name="request">包含文件ID的请求</param>
    /// <param name="context">gRPC 上下文</param>
    /// <returns>包含图片信息的响应</returns>
    /// <exception cref="ArgumentException">如果文件ID无效</exception>
    /// <exception cref="Exception">如果发生其他异常</exception>
    public override async Task<GetImageInfoResponse> GetImageInfo(
        GetImageInfoRequest request, ServerCallContext context)
    {
        try
        {
            var callerId = GetCallerId(context);
            if (!Guid.TryParse(request.FileId, out var fileId))
                return new GetImageInfoResponse { Success = false, ErrorMessage = "无效的文件ID" };

            var file = await _notFileService.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
                return new GetImageInfoResponse { Success = false, ErrorMessage = "文件不存在" };

            // S-08：私有图片仅所有者可查看
            if (!CanAccessFile(file, callerId))
                return new GetImageInfoResponse { Success = false, ErrorMessage = "无权访问此文件" };

            // 获取图片内容以解析尺寸
            var relativePath = file.FileUri.ToString().TrimStart('/').Replace("files/", "");

            int width = 0, height = 0;
            string format = "unknown";

            // S-09：GetContentAsync 会整读文件入内存，仅用于小图片尺寸解析；
            // 超过 100MB（与 Kestrel 请求体上限一致）的文件跳过尺寸解析，避免大文件整读入内存
            if (file.FileSize <= 100L * 1024 * 1024)
            {
                var (content, storageResponse) = await _storageService.GetContentAsync(relativePath);
                if (storageResponse.Success && content is not null)
                {
                    (width, height) = ImageValidator.GetDimensions(content);
                    format = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
                }
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
    /// <summary>
    /// 将域文件实体映射为gRPC文件信息协议缓冲区
    /// </summary>
    /// <param name="file">域文件实体</param>
    /// <returns>gRPC文件信息协议缓冲区</returns>
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
    /// <param name="identity">域文件身份枚举</param>
    /// <returns>gRPC文件身份协议缓冲区</returns>
    private static FileIdentityProto MapToProtoIdentity(DomainFileIdentity identity) => identity switch
    {
        DomainFileIdentity.FilePublic => FileIdentityProto.FilePublic,
        DomainFileIdentity.FilePrivate => FileIdentityProto.FilePrivate,
        DomainFileIdentity.FilePrivatePublic => FileIdentityProto.FilePrivatePublic,
        DomainFileIdentity.FilePasswordProtected => FileIdentityProto.FilePasswordProtected,
        _ => FileIdentityProto.FilePrivate
    };

    /// <summary>
    /// 将gRPC文件身份协议缓冲区映射为域文件身份枚举
    /// </summary>
    /// <param name="identity">gRPC文件身份协议缓冲区</param>
    /// <returns>域文件身份枚举</returns>
    private static DomainFileIdentity MapToDomainIdentity(FileIdentityProto identity) => identity switch
    {
        FileIdentityProto.FilePublic => DomainFileIdentity.FilePublic,
        FileIdentityProto.FilePrivate => DomainFileIdentity.FilePrivate,
        FileIdentityProto.FilePrivatePublic => DomainFileIdentity.FilePrivatePublic,
        FileIdentityProto.FilePasswordProtected => DomainFileIdentity.FilePasswordProtected,
        _ => DomainFileIdentity.FilePrivate
    };

    /// <summary>
    /// 将域文件类型枚举映射为gRPC文件类型协议缓冲区
    /// </summary>
    /// <param name="fileType">域文件类型枚举</param>
    /// <returns>gRPC文件类型协议缓冲区</returns>
    private static FileTypeProto MapToProtoFileType(DomainFileType fileType) => fileType switch
    {
        DomainFileType.FileImage => FileTypeProto.FileImage,
        DomainFileType.FileVideo => FileTypeProto.FileVideo,
        DomainFileType.FileAudio => FileTypeProto.FileAudio,
        DomainFileType.CompressFiles => FileTypeProto.FileCompress,
        DomainFileType.FileFile => FileTypeProto.FileOther,
        _ => FileTypeProto.FileOther,
    };

    /// <summary>
    /// 将gRPC文件类型协议缓冲区映射为域文件类型枚举
    /// </summary>
    /// <param name="fileType">gRPC文件类型协议缓冲区</param>
    /// <returns>域文件类型枚举</returns>
    private static DomainFileType MapToDomainFileType(FileTypeProto fileType) => fileType switch
    {
        FileTypeProto.FileImage => DomainFileType.FileImage,
        FileTypeProto.FileVideo => DomainFileType.FileVideo,
        FileTypeProto.FileAudio => DomainFileType.FileAudio,
        FileTypeProto.FileCompress => DomainFileType.CompressFiles,
        FileTypeProto.FileOther => DomainFileType.FileFile,
        _ => DomainFileType.FileFile,
    };

    /// <summary>
    /// 将域文件块上传状态枚举映射为gRPC文件块上传状态协议缓冲区
    /// </summary>
    /// <param name="status">域文件块上传状态枚举</param>
    /// <returns>gRPC文件块上传状态协议缓冲区</returns>
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

    /// <summary>
    /// 将gRPC文件块上传状态协议缓冲区映射为域文件块上传状态枚举
    /// </summary>
    /// <param name="status">gRPC文件块上传状态协议缓冲区</param>
    /// <returns>域文件块上传状态枚举</returns>
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

    /// <summary>
    /// 获取文件内容类型（S-17：.html/.htm/.svg 可被浏览器直接渲染，一律按 application/octet-stream 返回）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件内容类型</returns>
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

    /// <summary>
    /// 获取图片文件内容类型（S-17：.svg 可被浏览器直接渲染，一律按 application/octet-stream 返回）
    /// </summary>
    /// <param name="fileName">图片文件名</param>
    /// <returns>图片文件内容类型</returns>
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
            ".ico" => "image/x-icon",
            // S-17：禁止浏览器直接渲染
            ".svg" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}
