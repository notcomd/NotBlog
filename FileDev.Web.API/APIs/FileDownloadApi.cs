using Commons.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 文件下载 / 浏览 API（按业务域整合）：
/// <list type="bullet">
/// <item>匿名直链下载 <c>/files/{**path}</c>（独立于鉴权组，供 img/video 等无凭证请求直连）；</item>
/// <item>我的文件列表 <c>/api/filestorage/files/my</c>（当前用户已上传文件，按时间倒序分页）。</item>
/// </list>
/// 整合前分别位于 FileDownloadApi 与 MyFilesApis。
/// </summary>
public static class FileDownloadApi
{
    private const string ImageExts = ".jpg.jpeg.png.gif.bmp.webp.svg.ico";
    private const string VideoExts = ".mp4.avi.mkv.mov.wmv.flv.webm.m4v";

    /// <summary>挂载 /files 下载端点（独立于 /api/filestorage 鉴权组，供 img 等无凭证请求直连）。</summary>
    public static void MapFileDownloadApi(this WebApplication app)
    {
        app.MapGet("/files/{**path}", DownloadFileAsync);
    }

    /// <summary>挂载「我的文件」端点（/api/filestorage/files/my，需鉴权）。</summary>
    public static RouteGroupBuilder MapMyFilesApi(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/files");

        router.MapGet("/my", GetMyFilesAsync)
            .WithName("MyFiles")
            .WithDescription("获取当前用户已上传文件列表（按上传时间倒序；分页）");

        return router;
    }

    /// <summary>
    /// 匿名直链下载（<c>/files/{**path}</c>）——前端 &lt;img src&gt; 直接引用 fileUri 的访问通道。
    /// <para>
    /// 设计说明：
    /// - <b>无鉴权</b>（img 标签无法携带 Authorization 头）：文件名是 v7 GUID（128 位随机，不可枚举），
    ///   泄露 URL 即视为有权限访问（与 S3 预签名 URL 同思路）；相对路径统一经
    ///   NotFileStorageService.GetSafeFullPathAsync 清洗 + 校验根目录内（防路径穿越，S-16）。
    /// - <b>Range 支持</b>（enableRangeProcessing）：视频拖动 / 图片渐进加载依赖。
    /// - 缓存：文件内容不可变（文件名 = GUID，重传生成新文件），允许浏览器/代理缓存 1 天。
    /// </para>
    /// </summary>
    private static async Task<IResult> DownloadFileAsync(
        string path,
        [FromServices] INotFileStorageService storage,
        HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Results.NotFound();

        // path 形如 {userId:N}/{guid:N}{ext}，还原为存储相对路径（与元数据 FileUri 一致）
        var relativePath = FileApiHelpers.FileUriToRelativePath(new Uri("/files/" + path, UriKind.Relative));
        if (string.IsNullOrWhiteSpace(relativePath))
            return Results.NotFound();

        // 本端点匿名访问（img/video 直链），无登录上下文；命名空间以路径首段的属主用户为准，
        // 否则会落到默认命名空间导致非属主/匿名访问公开文件一律 404。
        var tenantId = FileApiHelpers.ResolveTenantFromFileKey(relativePath);
        var (stream, response) = await storage.GetContentStreamAsync(relativePath,
            context.RequestAborted, new StoreContext(null, tenantId));
        if (stream is null || !response.Success)
            return Results.NotFound();

        // 按扩展名推断 Content-Type；未知类型回退二进制流
        if (!new FileExtensionContentTypeProvider().TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        // 文件内容不可变（重传生成新 GUID 文件），允许缓存
        context.Response.Headers.CacheControl = "public, max-age=86400";

        // enableRangeProcessing：视频/图片大文件需要 Range 支持（拖动进度、渐进渲染）
        return Results.Stream(stream, contentType: contentType, enableRangeProcessing: true);
    }

    /// <summary>获取当前用户已上传文件列表（按上传时间倒序；分页）。</summary>
    private static async Task<IResult> GetMyFilesAsync(
        HttpContext context,
        [FromServices] FileServicesDi fileServicesDi,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = FileApiHelpers.GetUserId(context);
        if (userId == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            var files = (await fileServicesDi.NotFileService.GetFilesByUserIdAsync(userId.Value))
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.UploadTime);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var total = files.Count();
            var items = files
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    fileId = f.FileId,
                    name = f.FileName,
                    url = f.FileUri,
                    type = ResolveType(f.FileName),
                    size = f.FileSize,
                    createdAt = f.UploadTime
                });

            return Results.Ok(new { ok = true, items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            fileServicesDi.Logger.LogError(ex, "获取我的文件列表失败: {UserId}", userId.Value);
            return Results.Json(ApiResponseResult.Failure("获取文件列表失败", 500), statusCode: 500);
        }
    }

    /// <summary>由扩展名推断前端展示类型（image/video/doc）。</summary>
    private static string ResolveType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
        if (ImageExts.Contains('.' + ext)) return "image";
        if (VideoExts.Contains('.' + ext)) return "video";
        return "doc";
    }
}