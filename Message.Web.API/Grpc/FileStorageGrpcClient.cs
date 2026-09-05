using FileDev.Web.API.Grpc;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Options;

namespace Message.Web.API.Grpc;

/// <summary>
/// 文件存储 gRPC 客户端实现。
/// <para>
/// 通过 GrpcClientFactory 解析名为 "filedev-web-api" 的 gRPC 客户端
/// （该名称与 NotBlog.AppHost 中 FileDev.Web.API 的服务发现名称一致），
/// 封装了请求/响应处理、瞬时故障重试、断点续传与上传进度反馈。
/// </para>
/// </summary>
public class FileStorageGrpcClient : IFileStorageGrpcClient
{
    /// <summary>FileDev 文件服务在 Aspire 服务发现中的服务名称</summary>
    public const string ClientName = "filedev-web-api";

    /// <summary>可重试的瞬时性 gRPC 状态码（网络抖动、服务重启等场景）。
    /// 不含 ResourceExhausted：配额不足属于确定性业务失败，重试不会改变结果。</summary>
    private static readonly StatusCode[] RetryableStatusCodes =
    [
        StatusCode.Unavailable,
        StatusCode.DeadlineExceeded,
        StatusCode.Aborted,
        StatusCode.Internal
    ];

    private readonly GrpcClientFactory _clientFactory;
    private readonly IOptionsSnapshot<FileStorageGrpcOptions> _options;
    private readonly ILogger<FileStorageGrpcClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileStorageGrpcClient(
        GrpcClientFactory clientFactory,
        IOptionsSnapshot<FileStorageGrpcOptions> options,
        ILogger<FileStorageGrpcClient> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientFactory = clientFactory;
        _options = options;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UploadFileResult> UploadFileAsync(
        Guid userId, string fileName, byte[] content,
        string? description = null, string? expectedMd5 = null,
        Guid? contentId = null, ContentReferenceType? contentType = null,
        CancellationToken ct = default)
    {
        if (content.Length == 0)
            return new UploadFileResult(false, null, null, "", 0, "文件内容不能为空");

        try
        {
            var request = new FileDev.Web.API.Grpc.UploadFileRequest
            {
                UserId = userId.ToString(),
                FileName = fileName,
                FileContent = ByteString.CopyFrom(content),
                FileDescription = description ?? string.Empty,
                FileIdentity = FileIdentity.FilePrivate,
                ExpectedMd5 = expectedMd5 ?? string.Empty,
                ContentId = contentId?.ToString() ?? string.Empty,
                ContentType = MapContentType(contentType)
            };

            var response = await ExecuteWithRetryAsync(
                client => client.UploadFileAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"上传文件 {fileName}", ct);

            return new UploadFileResult(
                response.Success,
                Guid.TryParse(response.FileId, out var fileId) ? fileId : null,
                ToUri(response.FileUri),
                response.FileMd5,
                response.FileSize,
                response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UploadFileResult(false, null, null, "", 0, MapError(ex, "上传文件"));
        }
    }

    public async Task<UploadImageResult> UploadImageAsync(
        Guid userId, string fileName, byte[] content,
        string? description = null, bool validateFormat = true,
        Guid? contentId = null, ContentReferenceType? contentType = null,
        CancellationToken ct = default)
    {
        if (content.Length == 0)
            return new UploadImageResult(false, null, null, "", 0, 0, 0, "", "图片内容不能为空");

        try
        {
            var request = new UploadImageRequest
            {
                UserId = userId.ToString(),
                FileName = fileName,
                ImageContent = ByteString.CopyFrom(content),
                FileDescription = description ?? string.Empty,
                FileIdentity = FileIdentity.FilePrivate,
                ValidateFormat = validateFormat,
                MaxWidth = 3840,
                MaxHeight = 2160,
                ContentId = contentId?.ToString() ?? string.Empty,
                ContentType = MapContentType(contentType)
            };

            var response = await ExecuteWithRetryAsync(
                client => client.UploadImageAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"上传图片 {fileName}", ct);

            return new UploadImageResult(
                response.Success,
                Guid.TryParse(response.FileId, out var fileId) ? fileId : null,
                ToUri(response.FileUri),
                response.FileMd5,
                response.FileSize,
                response.Width,
                response.Height,
                response.Format,
                response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UploadImageResult(false, null, null, "", 0, 0, 0, "", MapError(ex, "上传图片"));
        }
    }

    public async Task<ChunkUploadInitResult> InitChunkUploadAsync(
        Guid userId, string fileName, long totalSize,
        string? fileMd5 = null, string? description = null, bool isPublic = false,
        CancellationToken ct = default)
    {
        if (totalSize <= 0)
            return new ChunkUploadInitResult(false, "", 0, 0, [], "文件大小必须大于0");

        try
        {
            var chunkSize = _options.Value.ChunkSize;
            var totalChunks = (int)Math.Ceiling((double)totalSize / chunkSize);

            var request = new InitChunkUploadRequest
            {
                UserId = userId.ToString(),
                FileName = fileName,
                TotalSize = totalSize,
                TotalChunks = totalChunks,
                FileMd5 = fileMd5 ?? string.Empty,
                FileType = ResolveFileType(fileName),
                FileIdentity = isPublic ? FileIdentity.FilePublic : FileIdentity.FilePrivate,
                FileDescription = description ?? string.Empty
            };

            var response = await ExecuteWithRetryAsync(
                client => client.InitChunkUploadAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"初始化分片上传 {fileName}", ct);

            if (!response.Success)
                return new ChunkUploadInitResult(false, "", 0, 0, [], response.ErrorMessage);

            // 初始化成功后查询已上传分片（断点续传基础：返回已上传索引）
            var uploaded = await QueryUploadedChunksAsync(response.FileKey, ct);
            return new ChunkUploadInitResult(
                true, response.FileKey, response.TotalChunks, response.ChunkSize, uploaded, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ChunkUploadInitResult(false, "", 0, 0, [], MapError(ex, "初始化分片上传"));
        }
    }

    public async Task<ChunkUploadResult> UploadChunkAsync(
        string fileKey, int chunkIndex, byte[] chunkData, string? chunkMd5 = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return new ChunkUploadResult(false, chunkIndex, "", "文件Key不能为空");
        if (chunkData.Length == 0)
            return new ChunkUploadResult(false, chunkIndex, "", "分片数据不能为空");

        try
        {
            var request = new UploadChunkRequest
            {
                FileKey = fileKey,
                ChunkIndex = chunkIndex,
                ChunkData = ByteString.CopyFrom(chunkData),
                ChunkMd5 = chunkMd5 ?? string.Empty
            };

            var response = await ExecuteWithRetryAsync(
                client => client.UploadChunkAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"上传分片 {fileKey}#{chunkIndex}", ct);

            return new ChunkUploadResult(
                response.Success, response.ChunkIndex, response.ChunkMd5,
                response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ChunkUploadResult(false, chunkIndex, "", MapError(ex, "上传分片"));
        }
    }

    public async Task<ChunkStatusResult> GetChunkStatusAsync(string fileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return new ChunkStatusResult(false, "", 0, [], "Pending", "文件Key不能为空");

        try
        {
            var request = new GetChunkStatusRequest { FileKey = fileKey };
            var response = await ExecuteWithRetryAsync(
                client => client.GetChunkStatusAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"查询分片状态 {fileKey}", ct);

            return new ChunkStatusResult(
                response.Success,
                response.FileKey,
                response.TotalChunks,
                response.UploadedChunks.ToArray(),
                response.Status.ToString(),
                response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ChunkStatusResult(false, fileKey, 0, [], "Failed", MapError(ex, "查询分片状态"));
        }
    }

    public async Task<MergeChunksResult> MergeChunksAsync(
        string fileKey, Guid userId, string? fileName = null, string? description = null,
        Guid? contentId = null, ContentReferenceType? contentType = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return new MergeChunksResult(false, null, null, "", 0, "文件Key不能为空");

        try
        {
            var request = new MergeChunksRequest
            {
                FileKey = fileKey,
                UserId = userId.ToString(),
                FileName = fileName ?? string.Empty,
                FileDescription = description ?? string.Empty,
                ContentId = contentId?.ToString() ?? string.Empty,
                ContentType = MapContentType(contentType)
            };

            var response = await ExecuteWithRetryAsync(
                client => client.MergeChunksAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"合并分片 {fileKey}", ct);

            return new MergeChunksResult(
                response.Success,
                Guid.TryParse(response.FileId, out var fileId) ? fileId : null,
                ToUri(response.FileUri),
                response.FileMd5,
                response.FileSize,
                response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new MergeChunksResult(false, null, null, "", 0, MapError(ex, "合并分片"));
        }
    }

    public async Task<CancelChunkUploadResult> CancelChunkUploadAsync(string fileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return new CancelChunkUploadResult(false, "文件Key不能为空");

        try
        {
            var request = new CancelChunkUploadRequest { FileKey = fileKey };
            var response = await ExecuteWithRetryAsync(
                client => client.CancelChunkUploadAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"取消分片上传 {fileKey}", ct);

            return new CancelChunkUploadResult(response.Success, response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CancelChunkUploadResult(false, MapError(ex, "取消分片上传"));
        }
    }

    public async Task<ChunkStatusResult> ResumeChunkUploadAsync(
        string fileKey, int totalChunks, int chunkSize,
        IReadOnlyDictionary<int, byte[]> chunks,
        IProgress<ChunkUploadProgress>? progress = null,
        CancellationToken ct = default)
    {
        // 1. 查询服务端已上传分片（断点续传核心：跳过已完成分片）
        var status = await GetChunkStatusAsync(fileKey, ct);
        if (!status.Success)
            return status;

        var uploaded = new HashSet<int>(status.UploadedChunks);
        var uploadedCount = uploaded.Count;

        // 2. 按索引升序上传缺失分片
        foreach (var (index, data) in chunks.OrderBy(kv => kv.Key))
        {
            ct.ThrowIfCancellationRequested();

            if (uploaded.Contains(index))
                continue; // 已上传，跳过

            var result = await UploadChunkAsync(fileKey, index, data, null, ct);
            if (!result.Success)
                return new ChunkStatusResult(false, fileKey, totalChunks, [.. uploaded], "Uploading",
                    $"分片 {index} 上传失败: {result.ErrorMessage}");

            uploaded.Add(index);
            uploadedCount = uploaded.Count;

            progress?.Report(new ChunkUploadProgress(
                fileKey, uploadedCount, totalChunks,
                totalChunks > 0 ? (double)uploadedCount / totalChunks * 100 : 0,
                index));
        }

        return new ChunkStatusResult(true, fileKey, totalChunks, [.. uploaded], "Uploading", null);
    }


    public async Task<FileInfoResult> GetFileInfoAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetFileInfoRequest { FileId = fileId.ToString() };

            var response = await ExecuteWithRetryAsync(
                client => client.GetFileInfoAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"获取文件信息 {fileId}", ct);

            if (!response.Success || response.FileInfo == null)
                return new FileInfoResult(false, null, null, "", 0, null, "", "",
                    response.Success ? "文件不存在" : response.ErrorMessage);

            var info = response.FileInfo;
            return new FileInfoResult(
                true,
                Guid.TryParse(info.FileId, out var fid) ? fid : null,
                Guid.TryParse(info.UserId, out var uid) ? uid : null,
                info.FileName,
                info.FileSize,
                ToUri(info.FileUri),
                info.FileMd5,
                info.FileType.ToString(),
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new FileInfoResult(false, null, null, "", 0, null, "", "", MapError(ex, "获取文件信息"));
        }
    }

    public async Task<DeleteFileResult> DeleteFileAsync(Guid fileId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new DeleteFileRequest
            {
                FileId = fileId.ToString(),
                UserId = userId.ToString()
            };

            var response = await ExecuteWithRetryAsync(
                client => client.DeleteFileAsync(request, BuildCallOptions(ct)).ResponseAsync,
                $"删除文件 {fileId}", ct);

            return new DeleteFileResult(response.Success, response.Success ? null : response.ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DeleteFileResult(false, MapError(ex, "删除文件"));
        }
    }


    public async Task<DownloadFileStreamResult> DownloadFileAsync(
        Guid fileId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var request = new DownloadFileRequest
            {
                FileId = fileId.ToString(),
                UserId = userId.ToString()
            };

            var call = CreateClient().DownloadFile(request, BuildCallOptions(ct));

            // ⚠️ 首块预取：gRPC 错误（认证/文件不存在/权限拒绝）在服务端流方法返回前抛出，
            // 客户端必须 await 首次 MoveNextAsync 才能收到——若把 ReadAllAsync 惰性返回，
            // 错误会在 HTTP 响应已 200 后的流式写入阶段爆发（客户端收到中断响应而非明确错误）。
            var enumerator = call.ResponseStream.ReadAllAsync(ct).GetAsyncEnumerator(ct);
            bool hasFirst;
            try
            {
                hasFirst = await enumerator.MoveNextAsync();
            }
            catch
            {
                await enumerator.DisposeAsync();
                throw; // 由外层 catch 映射为 DownloadFileStreamResult(false, ...)
            }

            async IAsyncEnumerable<byte[]> ReadChunks()
            {
                try
                {
                    if (hasFirst)
                    {
                        yield return enumerator.Current.ChunkData.ToByteArray();
                        hasFirst = false;
                    }
                    while (await enumerator.MoveNextAsync())
                    {
                        yield return enumerator.Current.ChunkData.ToByteArray();
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            }

            return new DownloadFileStreamResult(true, ReadChunks(), null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DownloadFileStreamResult(false, EmptyStream(), MapError(ex, "下载文件"));
        }
    }

    public async Task<DownloadImageStreamResult> DownloadImageAsync(
        Guid fileId, Guid userId, int? resizeWidth = null, int? resizeHeight = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new DownloadImageRequest
            {
                FileId = fileId.ToString(),
                UserId = userId.ToString(),
                ResizeWidth = resizeWidth ?? 0,
                ResizeHeight = resizeHeight ?? 0
            };

            var call = CreateClient().DownloadImage(request, BuildCallOptions(ct));

            // ⚠️ 首块预取（同 DownloadFileAsync：gRPC 错误前移，避免惰性流错误在 HTTP 200 后爆发）
            var enumerator = call.ResponseStream.ReadAllAsync(ct).GetAsyncEnumerator(ct);
            bool hasFirst;
            try
            {
                hasFirst = await enumerator.MoveNextAsync();
            }
            catch
            {
                await enumerator.DisposeAsync();
                throw;
            }

            async IAsyncEnumerable<byte[]> ReadChunks()
            {
                try
                {
                    if (hasFirst)
                    {
                        yield return enumerator.Current.ChunkData.ToByteArray();
                        hasFirst = false;
                    }
                    while (await enumerator.MoveNextAsync())
                    {
                        yield return enumerator.Current.ChunkData.ToByteArray();
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            }

            return new DownloadImageStreamResult(true, ReadChunks(), null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DownloadImageStreamResult(false, EmptyStream(), MapError(ex, "下载图片"));
        }
    }

    /// <summary>空响应流（失败结果占位，避免 API 层判空）</summary>
    private static async IAsyncEnumerable<byte[]> EmptyStream()
    {
        yield break;
    }

    // ─────────────────────────── 私有辅助方法 ───────────────────────────

    /// <summary>
    /// 执行 gRPC 调用并针对瞬时故障进行指数退避重试。
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<FileStorage.FileStorageClient, Task<T>> call,
        string operation, CancellationToken ct)
    {
        var maxRetries = Math.Max(0, _options.Value.RetryCount);
        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await call(CreateClient());
            }
            catch (RpcException ex) when (attempt < maxRetries && IsRetryable(ex.StatusCode))
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(Math.Min(500 * Math.Pow(2, attempt - 1), 4000));
                _logger.LogWarning(ex,
                    "gRPC 调用 {Operation} 失败（{StatusCode}），第 {Attempt}/{MaxRetries} 次重试，延迟 {Delay}ms",
                    operation, ex.StatusCode, attempt, maxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>判断 gRPC 状态码是否可重试</summary>
    private static bool IsRetryable(StatusCode statusCode) => RetryableStatusCodes.Contains(statusCode);

    /// <summary>
    /// 从当前 HttpContext 解析 Bearer token（HTTP 路径取 Authorization 头；SignalR 路径取 query access_token）。
    /// </summary>
    private static string? ResolveBearerToken(HttpContext? httpContext)
    {
        if (httpContext is null)
            return null;

        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        var queryToken = httpContext.Request.Query["access_token"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(queryToken) ? null : queryToken;
    }

    /// <summary>构建调用选项（超时 + 取消令牌）</summary>
    private CallOptions BuildCallOptions(CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(1, _options.Value.TimeoutSeconds));

        // S-08：FileDev gRPC 拦截器要求 Bearer JWT —— 转发当前请求/连接的原始 token。
        // ⚠️ 必须用 IHttpContextAccessor（Singleton + AsyncLocal）而非 ICurrentUserService：
        // NotMediator 的 mediator 是 Singleton，SendAsync 内部 CreateScope() 解析 handler——
        // handler 拿到的是新 scope 的 ICurrentUserService 实例（AccessToken 恒空），
        // 而 token 由 UserContextMiddleware 设在 HTTP 请求 scope 的实例上（captive scope 断链）。
        var headers = new Metadata();
        var token = ResolveBearerToken(_httpContextAccessor.HttpContext);
        if (!string.IsNullOrWhiteSpace(token))
        {
            headers.Add("Authorization", $"Bearer {token}");
        }
        else
        {
            _logger.LogWarning("[FileStorageGrpc] ⚠️ gRPC 调用缺少 Bearer token（HttpContext 中无 Authorization 头/access_token）");
        }

        return new CallOptions(
            headers: headers,
            cancellationToken: ct,
            deadline: DateTime.UtcNow.Add(timeout));
    }

    /// <summary>查询已上传分片索引列表</summary>
    private async Task<IReadOnlyList<int>> QueryUploadedChunksAsync(string fileKey, CancellationToken ct)
    {
        var status = await GetChunkStatusAsync(fileKey, ct);
        return status.Success ? status.UploadedChunks : [];
    }

    /// <summary>创建 gRPC 客户端（按服务发现名称解析）</summary>
    private FileStorage.FileStorageClient CreateClient()
        => _clientFactory.CreateClient<FileStorage.FileStorageClient>(ClientName);

    /// <summary>将异常映射为可读的错误信息</summary>
    private static string MapError(Exception ex, string operation)
    {
        if (ex is RpcException rpcEx)
            return $"文件服务调用失败({operation}): {rpcEx.Status.Detail}";

        _ = operation;
        return ex.Message;
    }

    /// <summary>将应用层内容引用类型映射为 gRPC 契约的 ContentType，空引用回退为 NONE</summary>
    private static FileDev.Web.API.Grpc.ContentType MapContentType(ContentReferenceType? contentType) =>
        contentType switch
        {
            ContentReferenceType.Post => FileDev.Web.API.Grpc.ContentType.Post,
            ContentReferenceType.Markdown => FileDev.Web.API.Grpc.ContentType.Markdown,
            ContentReferenceType.Video => FileDev.Web.API.Grpc.ContentType.Video,
            _ => FileDev.Web.API.Grpc.ContentType.None
        };

    /// <summary>解析相对 URI，空串返回 null</summary>
    private static Uri? ToUri(string uri) => string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri, UriKind.Relative);

    /// <summary>根据扩展名解析文件类型（与 FileDev 服务端保持一致）</summary>
    private static FileType ResolveFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
            ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.FileCompress,
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".md" or ".csv" or ".json" or ".xml" or ".html" or ".htm" => FileType.FileDocument,
            _ => FileType.FileOther
        };
    }
}
