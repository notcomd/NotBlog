using Commons.Result;
using Microsoft.AspNetCore.Mvc;


namespace FileDev.Web.API.APIs;

public static class FileChunkApis
{
    public static RouteGroupBuilder MapFileChunkApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/chunk");


        router.MapPost("/init", InitChunkUploadAsync)
            .WithMetadata(new RequestSizeLimitAttribute(1024 * 1024));

        router.MapPost("/upload", UploadChunkAsync)
            .WithMetadata(new RequestSizeLimitAttribute(11 * 1024 * 1024));

        router.MapGet("/status/{fileKey}", GetChunkStatusAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());

        router.MapPost("/merge", MergeChunksAsync);

        router.MapPost("/cancel/{fileKey}", CancelChunksAsync);

        return router;
    }

    private static IResult Unauthorized() => Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);
    private static IResult BadRequest(string error) => Results.Json(ApiResponseResult.Failure(error, 400), statusCode: 400);
    private static IResult InternalError() => Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);

    private static async Task<IResult> InitChunkUploadAsync(
        [FromServices] HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] ChunkInitRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileChunkApis");
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
        var logger = loggerFactory.CreateLogger("FileChunkApis");
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

    private static async Task<IResult> GetChunkStatusAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        string fileKey,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileChunkApis");
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

    private static async Task<IResult> MergeChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] MergeRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileChunkApis");
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

    private static async Task<IResult> CancelChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        string fileKey,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("FileChunkApis");
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



    public record MergeRequest(string FileKey);

    public record DedupRequest(string FileMd5, long FileSize);
}
