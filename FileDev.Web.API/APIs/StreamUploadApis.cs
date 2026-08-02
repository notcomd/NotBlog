using FileDev.Domain.Entities;
using FileDev.Web.API.Application.Command;
using Microsoft.AspNetCore.Mvc;
using NotMediator;

namespace FileDev.Web.API.APIs;

public static class StreamUploadApis
{
    public static RouteGroupBuilder MapStreamUploadApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/stream");

        // S-09：移除 DisableRequestSizeLimit，改用端点级请求体上限（与 Kestrel 100MB 一致）
        router.MapPost("/upload", StreamUploadAsync)
            .WithMetadata(new RequestSizeLimitAttribute(100 * 1024 * 1024));

        return router;
    }

    public static RouteGroupBuilder MapDedupApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/dedup");

        router.MapPost("/check", CheckDedupAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());

        return router;
    }

    private static async Task<IResult> StreamUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        CancellationToken ct)
    {
        try
        {
            var userId = FileChunkApis.GetUserId(context);
            if (userId == null)
                return Results.Json(new { error = "未认证" }, statusCode: 401);

            var fileName = context.Request.Headers["X-File-Name"].FirstOrDefault()
                ?? "unnamed.bin";

            if (!long.TryParse(context.Request.Headers["X-File-Size"].FirstOrDefault(), out var fileSize))
                fileSize = context.Request.ContentLength ?? 0;

            // S-09：读取请求体前先校验声明大小，拒绝超限请求（避免整读超大文件）
            if (fileSize <= 0 || fileSize > storageOptions.Value.MaxFileSize)
                return Results.Json(new
                {
                    error = $"文件大小必须在 (0, {storageOptions.Value.MaxFileSize / 1024 / 1024}MB] 范围内"
                }, statusCode: 400);

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

            return Results.Json(new { success = true, fileName = result.FileName, fileUri = result.FileUri.ToString() });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> CheckDedupAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromBody] FileChunkApis.DedupRequest request,
        CancellationToken ct)
    {
        // F-09.1：秒传命中需绑定当前调用者，从 JWT 解析用户 id
        var userId = FileChunkApis.GetUserId(context);
        if (userId == null)
            return Results.Json(new { error = "未认证" }, statusCode: 401);

        var cmd = new DeduplicateFileCommand
        {
            FileMd5 = request.FileMd5,
            FileSize = request.FileSize,
            UserId = userId.Value
        };
        var result = await mediator.SendAsync(cmd, ct);
        return Results.Json(new
        {
            exists = result.Exists,
            fileId = result.FileId,
            fileUri = result.FileUri
        });
    }
}
