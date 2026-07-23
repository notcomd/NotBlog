
using FileDev.Web.API.Application.Command;
using Microsoft.AspNetCore.Mvc;


namespace FileDev.Web.API.APIs;

public static class FileChunkApis
{
    public static RouteGroupBuilder MapFileChunkApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/chunk");

        router.MapPost("/init", InitChunkUploadAsync)
            .WithMetadata(new DisableRequestSizeLimitAttribute());

        router.MapPost("/upload", UploadChunkAsync)
            .WithMetadata(new DisableRequestSizeLimitAttribute());

        router.MapGet("/status/{fileKey}", GetChunkStatusAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());

        router.MapPost("/merge", MergeChunksAsync);

        router.MapPost("/cancel/{fileKey}", CancelChunksAsync);

        return router;
    }

    private static async Task<IResult> InitChunkUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromBody] ChunkInitRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId(context);
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            var fileType = ResolveFileType(ext);

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
                fileKey = result.FileKey,
                totalChunks = result.TotalChunks,
                chunkSize = result.ChunkSize,
                uploadedChunks = result.UploadedChunks.ToList()
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> UploadChunkAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromForm] string fileKey,
        [FromForm] int chunkIndex,
        IFormFile chunkContent,
        CancellationToken ct)
    {
        try
        {
            using var ms = new MemoryStream();
            await chunkContent.CopyToAsync(ms, ct);
            var content = ms.ToArray();

            var cmd = new UploadChunkCommand
            {
                FileKey = fileKey,
                ChunkIndex = chunkIndex,
                ChunkContent = content
            };

            await mediator.SendAsync(cmd, ct);
            return Results.Json(new { chunkIndex, received = true, size = content.Length });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> GetChunkStatusAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        string fileKey,
        CancellationToken ct)
    {
        try
        {
            var query = new ChunkStatusQuery { FileKey = fileKey };
            var result = await mediator.SendAsync(query, ct);
            return Results.Json(new
            {
                fileKey = result.FileKey,
                totalChunks = result.TotalChunks,
                uploadedChunks = result.UploadedChunks.ToList(),
                isComplete = result.IsComplete,
                status = result.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 404);
        }
    }

    private static async Task<IResult> MergeChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromBody] MergeRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
                return Results.Json(new { error = "未认证" }, statusCode: 401);

            var cmd = new MergeChunksCommand
            {
                UserId = userId.Value,
                FileKey = request.FileKey
            };

            var result = await mediator.SendAsync(cmd, ct);
            return Results.Json(new
            {
                success = true,
                fileKey = request.FileKey,
                fileName = result.FileName,
                fileUri = result.FileUri.ToString()
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> CancelChunksAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        string fileKey,
        CancellationToken ct)
    {
        var cmd = new CancelChunksCommand { FileKey = fileKey };
        await mediator.SendAsync(cmd, ct);
        return Results.Json(new { cancelled = true, fileKey });
    }

    // ---- 共享辅助方法 ----

    internal static Guid? GetUserId(HttpContext context)
    {
        var claim = context.User.Claims.FirstOrDefault(x => x.Type == "id")
            ?? context.User.Claims.FirstOrDefault(x =>
                x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }

    internal static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };

    // ---- 请求模型 ----

    public record ChunkInitRequest(
        string FileName,
        long TotalSize,
        int ChunkSize = 5242880,
        string? FileMd5 = null,
        bool IsPublic = false,
        string? Description = null);

    public record MergeRequest(string FileKey);

    public record DedupRequest(string FileMd5, long FileSize);
}
