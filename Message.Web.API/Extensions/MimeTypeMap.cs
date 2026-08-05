namespace Message.Web.API.Extensions;

/// <summary>
/// 扩展名 → MIME 类型映射工具。
/// <para>
/// 用途：FileDev gRPC 的 FileInfo 不返回 MIME 类型，Message 侧在发送附件消息时
/// 按文件名扩展名推断 MIME（与 <c>FileStorageGrpcClient.ResolveFileType</c> 的分类保持一致）。
/// </para>
/// </summary>
public static class MimeTypeMap
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // 图片
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
        [".gif"] = "image/gif", [".bmp"] = "image/bmp", [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml", [".ico"] = "image/x-icon",
        // 视频
        [".mp4"] = "video/mp4", [".avi"] = "video/x-msvideo", [".mkv"] = "video/x-matroska",
        [".mov"] = "video/quicktime", [".wmv"] = "video/x-ms-wmv", [".flv"] = "video/x-flv",
        [".webm"] = "video/webm",
        // 音频
        [".mp3"] = "audio/mpeg", [".wav"] = "audio/wav", [".ogg"] = "audio/ogg",
        [".flac"] = "audio/flac", [".aac"] = "audio/aac", [".wma"] = "audio/x-ms-wma",
        [".m4a"] = "audio/mp4",
        // 压缩包
        [".zip"] = "application/zip", [".rar"] = "application/vnd.rar",
        [".7z"] = "application/x-7z-compressed", [".tar"] = "application/x-tar",
        [".gz"] = "application/gzip", [".bz2"] = "application/x-bzip2",
        // 文档
        [".pdf"] = "application/pdf", [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain", [".md"] = "text/markdown", [".csv"] = "text/csv",
        [".json"] = "application/json", [".xml"] = "application/xml",
        [".html"] = "text/html", [".htm"] = "text/html"
    };

    /// <summary>
    /// 根据文件名推断 MIME 类型；无法识别时返回 <c>application/octet-stream</c>。
    /// </summary>
    public static string FromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "application/octet-stream";

        var ext = Path.GetExtension(fileName);
        return Map.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }
}
