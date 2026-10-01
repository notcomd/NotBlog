using Commons.Result;
using Microsoft.AspNetCore.Mvc;
using NotMediator;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 文件上传 API（按业务域整合）：
/// <list type="bullet">
/// <item>分片上传 <c>/chunk</c>（init / upload / status / merge / cancel）；</item>
/// <item>流式上传 <c>/stream/upload</c>；</item>
/// <item>秒传校验 <c>/dedup/check</c>。</item>
/// </list>
/// 挂载于 <c>/api/filestorage</c> 需 JWT 认证的分组下；整合前分别为 FileChunkApis 与 StreamUploadApis。
/// </summary>
public static class FileUploadApi
{
    /// <summary>挂载文件上传相关端点（分片 /chunk、流式 /stream、秒传 /dedup 三个子分组）。</summary>
    public static RouteGroupBuilder MapFileUploadApi(this RouteGroupBuilder routeGroupBuilder)
    {
        MapChunkEndpoints(routeGroupBuilder);
        MapStreamEndpoints(routeGroupBuilder);
        MapDedupEndpoints(routeGroupBuilder);
        return routeGroupBuilder;
    }

    /// <summary>分片上传子分组 /chunk。</summary>
    private static void MapChunkEndpoints(RouteGroupBuilder group)
    {
        var router = group.MapGroup("/chunk");

        router.MapPost("/init", InitChunkUploadAsync)
            .WithMetadata(new RequestSizeLimitAttribute(1024 * 1024));

        router.MapPost("/upload", UploadChunkAsync)
            .WithMetadata(new RequestSizeLimitAttribute(11 * 1024 * 1024));

        router.MapGet("/status/{fileKey}", GetChunkStatusAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());

        router.MapPost("/merge", MergeChunksAsync);

        router.MapPost("/cancel/{fileKey}", CancelChunksAsync);
    }

    /// <summary>流式上传子分组 /stream。</summary>
    private static void MapStreamEndpoints(RouteGroupBuilder group)
    {
        var router = group.MapGroup("/stream");

        // S-09：使用端点级请求体上限（与 Kestrel 100MB 一致）
        router.MapPost("/upload", StreamUploadAsync)
            .WithMetadata(new RequestSizeLimitAttribute(100 * 1024 * 1024));
    }

    /// <summary>秒传校验子分组 /dedup。</summary>
    private static void MapDedupEndpoints(RouteGroupBuilder group)
    {
        var router = group.MapGroup("/dedup");

        router.MapPost("/check", CheckDedupAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());
    }

    private static IResult Unauthorized() => Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);
    private static IResult BadRequest(string error) => Results.Json(ApiResponseResult.Failure(error, 400), statusCode: 400);
    private static IResult InternalError() => Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);

    /// <summary>初始化分片上传。</summary>
    private static async Task<IResult> InitChunkUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] ChunkInitRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (request == null)
                return BadRequest("请求体不能为空");
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("文件名不能为空");
            if (request.TotalSize <= 0 || request.TotalSize > storageOptions.Value.MaxFileSize)
                return BadRequest($"文件大小必须在 (0, {storageOptions.Value.MaxFileSize / 1024 / 1024}MB] 范围内");
            if (request.ChunkSize <= 0)
                return BadRequest("分片大小必须大于0");

            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            var fileType = FileApiHelpers.ResolveFileType(ext);

            var cmd = new ChunkUploadInitCommand
            {
                UserId = userId.Value,
                FileName = request.FileName,
                TotalSize = request.TotalSize,
                ChunkSize = request.ChunkSize,
                FileMd5 = request.FileMd5 ?? string.Empty,
                FileType = fileType,
                FileIdentity = request.IsPublic ? FileIdentity.FilePublic : FileIdentity.FilePrivate,
                FileDescription = request.Description
            };
            var result = await mediator.SendAsync(cmd, ct);
            return Results.Json(new
            {
                ok = true,
                fileKey = result.FileKey,
                totalChunks = result.TotalChunks,
                chunkSize = result.ChunkSize,
                uploadedChunks = result.UploadedChunks.ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "分片上传初始化失败: FileName={FileName}", request?.FileName);
            return InternalError();
        }
    }

    /// <summary>上传单个分片。</summary>
    private static async Task<IResult> UploadChunkAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        [FromServices] ILoggerFactory loggerFactory,
        [FromForm] string fileKey,
        [FromForm] int chunkIndex,
        [FromForm] IFormFile chunkContent,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(fileKey))
                return BadRequest("fileKey 不能为空");
            if (chunkIndex < 0)
                return BadRequest("chunkIndex 不能为负数");

            if (chunkContent is null || chunkContent.Length == 0)
                return BadRequest("分片数据不能为空");

            if (chunkContent.Length > storageOptions.Value.ChunkFileSize * 2)
                return BadRequest("分片数据超出大小限制");

            using var ms = new MemoryStream();
            await chunkContent.CopyToAsync(ms, ct);
            var content = ms.ToArray();

            var cmd = new UploadChunkCommand
            {
                UserId = userId.Value,
                FileKey = fileKey,
                ChunkIndex = chunkIndex,
                ChunkContent = content
            };

            await mediator.SendAsync(cmd, ct);
            return Results.Json(new { ok = true, chunkIndex, received = true, size = content.Length });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "分片上传失败: FileKey={FileKey}, ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
            return InternalError();
        }
    }

    /// <summary>查询分片上传状态。</summary>
    private static async Task<IResult> GetChunkStatusAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        string fileKey,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(fileKey))
                return BadRequest("fileKey 不能为空");

            var query = new ChunkStatusQuery { FileKey = fileKey, UserId = userId.Value };
            var result = await mediator.SendAsync(query, ct);
            return Results.Json(new
            {
                ok = true,
                fileKey = result.FileKey,
                totalChunks = result.TotalChunks,
                uploadedChunks = result.UploadedChunks.ToList(),
                isComplete = result.IsComplete,
                status = result.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查询分片状态失败: FileKey={FileKey}", fileKey);
            return InternalError();
        }
    }

    /// <summary>合并分片为完整文件。</summary>
    private static async Task<IResult> MergeChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] MergeRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.FileKey))
                return BadRequest("fileKey 不能为空");

            var cmd = new MergeChunksCommand
            {
                UserId = userId.Value,
                FileKey = request.FileKey
            };

            var result = await mediator.SendAsync(cmd, ct);
            return Results.Json(new
            {
                ok = true,
                fileKey = request.FileKey,
                fileName = result.FileName,
                fileUri = result.FileUri.ToString()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "合并分片失败: FileKey={FileKey}", request?.FileKey);
            return InternalError();
        }
    }

    /// <summary>取消分片上传。</summary>
    private static async Task<IResult> CancelChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        string fileKey,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(fileKey))
                return BadRequest("fileKey 不能为空");

            var cmd = new CancelChunksCommand { FileKey = fileKey, UserId = userId.Value };
            await mediator.SendAsync(cmd, ct);
            return Results.Json(new { ok = true, cancelled = true, fileKey });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "取消分片上传失败: FileKey={FileKey}", fileKey);
            return InternalError();
        }
    }

    /// <summary>流式上传（请求头 X-File-Name / X-File-Size 描述文件）。</summary>
    private static async Task<IResult> StreamUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        var fileName = "unnamed.bin";
        long fileSize = 0;
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            // 从请求头解析文件名，缺失或非法统一拒绝
            var fileNameHeader = context.Request.Headers["X-File-Name"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(fileNameHeader))
                return BadRequest("X-File-Name 请求头不能为空");
            fileName = fileNameHeader;

            // 路径穿越防御：禁止包含路径分隔符或父目录引用的文件名
            if (fileName.IndexOfAny(['/', '\\']) >= 0 || fileName.Contains("..", StringComparison.Ordinal))
                return BadRequest("文件名不能包含路径分隔符");

            if (!long.TryParse(context.Request.Headers["X-File-Size"].FirstOrDefault(), out fileSize))
                fileSize = context.Request.ContentLength ?? 0;

            // S-09：读取请求体前先校验声明大小，拒绝超限请求（避免整读超大文件）
            if (fileSize <= 0 || fileSize > storageOptions.Value.MaxFileSize)
                return BadRequest($"文件大小必须在 (0, {storageOptions.Value.MaxFileSize / 1024 / 1024}MB] 范围内");

            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms, ct);
            ms.Position = 0;

            var cmd = new StreamUploadCommand
            {
                UserId = userId.Value,
                FileName = fileName,
                FileStream = ms,
                FileSize = fileSize,
                FileIdentity = FileIdentity.FilePrivate,
            };

            var result = await mediator.SendAsync(cmd, ct);

            return Results.Json(new { ok = true, fileName = result.FileName, fileUri = result.FileUri.ToString() });
        }
        catch (Exception ex)
        {
            // 完整异常仅记录服务端日志，客户端返回安全通用消息
            logger.LogError(ex, "流式上传失败: FileName={FileName}, Size={Size}", fileName, fileSize);
            return InternalError();
        }
    }

    /// <summary>秒传校验（按 MD5 + 大小命中既有文件）。</summary>
    private static async Task<IResult> CheckDedupAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] DedupRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileUploadApi");
        try
        {
            // F-09.1：秒传命中需绑定当前调用者，从 JWT 解析用户 id
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            if (request == null)
                return BadRequest("请求体不能为空");
            if (string.IsNullOrWhiteSpace(request.FileMd5))
                return BadRequest("FileMd5 不能为空");
            if (request.FileSize < 0)
                return BadRequest("FileSize 不能为负数");

            var cmd = new DeduplicateFileCommand
            {
                FileMd5 = request.FileMd5,
                FileSize = request.FileSize,
                UserId = userId.Value
            };
            var result = await mediator.SendAsync(cmd, ct);
            return Results.Json(new
            {
                ok = true,
                exists = result.Exists,
                fileId = result.FileId,
                fileUri = result.FileUri
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "秒传检查失败: Md5={Md5}", request?.FileMd5);
            return InternalError();
        }
    }

    /// <summary>合并分片请求体。</summary>
    public record MergeRequest(string FileKey);

    /// <summary>秒传校验请求体。</summary>
    public record DedupRequest(string FileMd5, long FileSize);
}