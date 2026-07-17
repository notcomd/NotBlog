using System.ComponentModel.DataAnnotations;
using FileDev.Domain.Entities;
using FileDev.Web.API.Application.Command;
using Microsoft.AspNetCore.Mvc;
using NotMediator;

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

    public static RouteGroupBuilder MapStreamUploadApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/stream");

        router.MapPost("/upload", StreamUploadAsync)
            //.WithMetadata(new IgnoreAntiforgeryTokenAttribute())
            .WithMetadata(new DisableRequestSizeLimitAttribute());

        return router;
    }

    public static RouteGroupBuilder MapDedupApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/dedup");

        router.MapPost("/check", CheckDedupAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());

        return router;
    }

    /// <summary>
    /// 初始化文件分块上传
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="request">初始化请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
    private static async Task<IResult> InitChunkUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromBody] ChunkInitRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId(context);
            //if (userId == null)
            //  return Results.Json(new { error = "未认证或用户ID无效" }, statusCode: 401);

            /// 解析文件类型
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
           // var identityCheckCmd = new IdentifiedCommand<ChunkUploadInitCommand, bool>(Guid.CreateVersion7(),cmd);
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

    /// <summary>
    /// 上传文件分块
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="fileKey">文件分块上传键</param>
    /// <param name="chunkIndex">分块索引</param>
    /// <param name="chunkContent">分块内容</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
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

    /// <summary>
    /// 获取文件分块上传状态
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">查询发送器</param>
    /// <param name="fileKey">文件分块上传键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
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

    /// <summary>
    /// 合并文件分块
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="request">合并请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
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

    /// <summary>
    /// 取消文件分块上传
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="fileKey">文件分块上传键</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
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

    /// <summary>
    /// 上传文件流
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
    private static async Task<IResult> StreamUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = GetUserId(context);
            //if (userId == null)
                //return Results.Json(new { error = "未认证" }, statusCode: 401);

            var fileName = context.Request.Headers["X-File-Name"].FirstOrDefault()
                ?? "unnamed.bin";

            if (!long.TryParse(context.Request.Headers["X-File-Size"].FirstOrDefault(), out var fileSize))
                fileSize = context.Request.ContentLength ?? 0;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var fileType = ResolveFileType(ext);

            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms, ct);
            ms.Position = 0;

            var cmd = new StreamUploadCommand
            {
                UserId = userId.Value,
                FileName = fileName,
                FileStream = ms,
                FileSize = fileSize,
                FileType = fileType
            };

            var result = await mediator.SendAsync(cmd, ct);
            return Results.Json(new { success = true, fileName = result.FileName, fileUri = result.FileUri.ToString() });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
    }

    /// <summary>
    /// 检查文件是否已存在
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="mediator">命令发送器</param>
    /// <param name="request">检查请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns></returns>
    private static async Task<IResult> CheckDedupAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromBody] DedupRequest request,
        CancellationToken ct)
    {
        var cmd = new DeduplicateFileCommand
        {
            FileMd5 = request.FileMd5,
            FileSize = request.FileSize
        };
        var result = await mediator.SendAsync(cmd, ct);
        return Results.Json(new
        {
            exists = result.Exists,
            fileId = result.FileId,
            fileUri = result.FileUri
        });
    }

    /// <summary>
    /// 获取用户ID
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns></returns>
    private static Guid? GetUserId(HttpContext context)
    {
        var claim = context.User.Claims.FirstOrDefault(x => x.Type == "id")
            ?? context.User.Claims.FirstOrDefault(x =>
                x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }

    /// <summary>
    /// 解析文件类型
    /// </summary>
    /// <param name="ext">文件扩展名</param>
    /// <returns></returns>
    private static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };

    /// <summary>
    /// 初始化文件分块上传请求
    /// </summary>
    /// <param name="FileName">文件名</param>
    /// <param name="TotalSize">总文件大小</param>
    /// <param name="ChunkSize">分块大小</param>
    /// <param name="FileMd5">文件MD5值</param>
    /// <param name="IsPublic">是否公开</param>
    /// <param name="Description">文件描述</param>
    /// <returns></returns>
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
