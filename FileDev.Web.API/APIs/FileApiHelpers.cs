
namespace FileDev.Web.API.APIs;

/// <summary>
/// 文件存储 HTTP API 的共享静态辅助方法
/// （GetUserId/ResolveFileType/BuildFileKey/FileUriToRelativePath 原散落在 FileChunkApis、FileStrongApi、
/// FileStorageServiceGRPC、StreamUploadCommandHandler、MergeChunksCommandHandler、FileDeletedEventHandler 等多处，
/// 统一收敛至此，见审查 #20）
/// </summary>
internal static class FileApiHelpers
{
    /// <summary>下载 URI 前缀，与 BuildFileUri 配对使用</summary>
    internal const string FileUriPrefix = "/files/";

    /// <summary>
    /// 获取用户ID
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>用户ID</returns>
    internal static Guid? GetUserId(HttpContext context)
    {
        var claim = context.User.Claims.FirstOrDefault(x => x.Type == "id")
            ?? context.User.Claims.FirstOrDefault(x =>
                x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }

    /// <summary>
    /// S-14：幂等键由客户端显式传入（请求头 X-Idempotency-Key）。
    /// 缺失或非合法 GUID 时回退为随机键（该请求无幂等保证，不影响其他请求）。
    /// 与 Identity 模块 IdentityApis.GetIdempotencyKey 保持同语义。
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>客户端幂等键；未提供或非法时回退随机键</returns>
    internal static Guid GetIdempotencyKey(HttpContext context)
    {
        var header = context.Request.Headers["X-Idempotency-Key"].ToString();
        return Guid.TryParse(header, out var key) ? key : Guid.CreateVersion7();
    }

    /// <summary>
    /// 解析文件类型
    /// </summary>
    /// <param name="ext">文件扩展名</param>
    /// <returns>文件类型</returns>
    internal static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };

    /// <summary>
    /// 生成统一的存储相对路径（同时用作 fileKey）。
    /// 形如 <c>{userId:N}/{guid:N}{ext}</c>，物理路径与下载 URI（/files/{fileKey}）一一对应。
    /// </summary>
    /// <param name="userId">文件归属用户 ID</param>
    /// <param name="ext">小写扩展名（含点，如 ".jpg"）</param>
    /// <returns>存储相对路径/fileKey</returns>
    internal static string BuildFileKey(Guid userId, string ext)
        => $"{userId:N}/{Guid.CreateVersion7():N}{ext}";


    /// <summary>
    /// 由 fileKey 构建相对下载 URI（<c>/files/{fileKey}</c>）。
    /// </summary>
    internal static Uri BuildFileUri(string fileKey)
        => new($"{FileUriPrefix}{fileKey}", UriKind.Relative);

    /// <summary>
    /// 将下载 URI（<c>/files/{fileKey}</c>）还原为存储相对路径/fileKey。
    /// 兼容带前导斜杠与无前导斜杠两种形式。
    /// </summary>
    internal static string FileUriToRelativePath(Uri fileUri)
    {
        var s = fileUri.ToString();
        if (s.StartsWith(FileUriPrefix, StringComparison.Ordinal))
            return s[FileUriPrefix.Length..];
        return s.TrimStart('/').Replace("files/", string.Empty, StringComparison.Ordinal);
    }
    
}
