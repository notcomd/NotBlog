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

    private static IResult Unauthorized() => Results.Json(new { ok = false, error = "未认证" }, statusCode: 401);
    private static IResult BadRequest(string error) => Results.Json(new { ok = false, error }, statusCode: 400);
    private static IResult InternalError() => Results.Json(new { ok = false, error = "请求处理失败" }, statusCode: 500);

    private static async Task<IResult> StreamUploadAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] IOptionsSnapshot<NotFileStorageOptions> storageOptions,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("StreamUploadApis");
        var fileName = "unnamed.bin";
        long fileSize = 0;
        try
        {
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            // Major：从请求头解析文件名，缺失或非法统一拒绝
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
            // #11：完整异常仅记录服务端日志，客户端返回安全通用消息
            logger.LogError(ex, "流式上传失败: FileName={FileName}, Size={Size}", fileName, fileSize);
            return InternalError();
        }
    }

    private static async Task<IResult> CheckDedupAsync(
        HttpContext context,
        [FromServices] INotMediator mediator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] FileChunkApis.DedupRequest request,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("StreamUploadApis");
        try
        {
            // F-09.1：秒传命中需绑定当前调用者，从 JWT 解析用户 id
            var userId = FileApiHelpers.GetUserId(context);
            if (userId == null)
                return Unauthorized();

            // Major：参数合法性校验
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
            // #11：完整异常仅记录服务端日志，客户端返回安全通用消息
            logger.LogError(ex, "秒传检查失败: Md5={Md5}", request?.FileMd5);
            return InternalError();
        }
    }
}
