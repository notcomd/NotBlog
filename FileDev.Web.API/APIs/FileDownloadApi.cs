using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 文件下载端点（<c>/files/{**path}</c>）——前端 &lt;img src&gt; 直接引用 fileUri 的访问通道。
/// <para>
/// 设计说明：
/// - <b>无鉴权</b>（img 标签无法携带 Authorization 头）：文件名是 v7 GUID（128 位随机，不可枚举），
///   泄露 URL 即视为有权限访问（与 S3 预签名 URL 同思路）；相对路径统一经
///   NotFileStorageService.GetSafeFullPathAsync 清洗 + 校验根目录内（防路径穿越，S-16）。
/// - <b>Range 支持</b>（enableRangeProcessing）：视频拖动 / 图片渐进加载依赖。
/// - 缓存：文件内容不可变（文件名 = GUID，重传生成新文件），允许浏览器/代理缓存 1 天。
/// </para>
/// </summary>
public static class FileDownloadApi
{
    /// <summary>挂载 /files 下载端点（独立于 /api/filestorage 鉴权组，供 img 等无凭证请求直连）。</summary>
    public static void MapFileDownloadApi(this WebApplication app)
    {
        app.MapGet("/files/{**path}", DownloadFileAsync);
    }

    private static async Task<IResult> DownloadFileAsync(
        string path,
        [FromServices] INotFileStorageService storage,
        [FromServices] HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Results.NotFound();

        // path 形如 {userId:N}/{guid:N}{ext}，还原为存储相对路径（与元数据 FileUri 一致）
        var relativePath = FileApiHelpers.FileUriToRelativePath(new Uri("/files/" + path, UriKind.Relative));
        if (string.IsNullOrWhiteSpace(relativePath))
            return Results.NotFound();

        var (stream, response) = await storage.GetContentStreamAsync(relativePath);
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
}
