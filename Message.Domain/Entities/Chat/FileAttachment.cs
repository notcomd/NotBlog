
namespace Message.Domain.Entities.Chat;

/// <summary>
///  文件附件
///  <para>
///  DDD 说明：文件附件是 <see cref="Message"/> 聚合内的<b>实体</b>（由聚合根通过
///  <see cref="Message.AddAttachment"/> 统一管理），因此不声明为聚合根。
///  其生命周期始终隶属于所属消息，独立仓储仅用于查询投影场景。
///  </para>
/// </summary>
public class FileAttachment : Entity<Guid>
{
    /// <summary>
    /// 创建文件附件
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="fileId">FileDev 文件ID（附件引用的文件唯一标识，转发/群聊可共享同一 fileId）</param>
    /// <param name="fileName">文件名</param>
    /// <param name="fileType">文件类型（MIME 字符串，如 image/png）</param>
    /// <param name="fileSize">文件大小</param>
    /// <param name="fileUri">文件URI（FileDev 返回的 FileUri）</param>
    /// <param name="mimeType">MIME 类型（可选，缺省时与 fileType 一致）</param>
    /// <param name="thumbnailUri">缩略图URI（可选，图片/视频消息）</param>
    /// <returns>文件附件</returns>
    public FileAttachment(Guid messageId, Guid fileId, string fileName, string fileType, long fileSize, Uri fileUri,
        string? mimeType = null, Uri? thumbnailUri = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");
        if (fileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空", nameof(fileId));

        // 主键保持 Guid.Empty（IsTransient=true）：EF DetectChanges 才能识别为「新增」实体，
        // INSERT 时由 EF 生成主键。修复（2026-08-15）：此前此处生成 Guid，导致加入聚合导航
        // 集合的新附件被 EF 判定为「已存在」（Modified）→ SaveChanges 生成 UPDATE 影响 0 行
        // → DbUpdateConcurrencyException。
        MessageId = messageId;
        FileId = fileId;
        FileName = fileName;
        FileType = fileType;
        FileSize = fileSize;
        FileUri = fileUri;
        MimeType = mimeType ?? fileType;
        ThumbnailUri = thumbnailUri;
        UploadTime = DateTime.UtcNow;
        DownloadCount = 0;
        IsDeleted = false;
    }

    /// <summary>
    /// 创建文件附件
    /// </summary>
    /// <returns>文件附件</returns>
    private FileAttachment()
    {
        UploadTime = DateTime.UtcNow;
        DownloadCount = 0;
        IsDeleted = false;
    }

    /// <summary>
    /// 文件附件ID
    /// </summary>
    public Guid AttachmentId { get; init; }

    /// <summary>
    /// 消息ID
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// FileDev 文件ID（附件引用的文件唯一标识；转发/群聊共享同一文件时多条附件记录可指向同一 FileId）
    /// </summary>
    public Guid FileId { get; private set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; private set; } = null!;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string FileType { get; private set; } = null!;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// 文件URI
    /// </summary>
    public Uri FileUri { get; private set; } = null!;

    /// <summary>
    /// 缩略图URI
    /// </summary>
    public Uri? ThumbnailUri { get; private set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; private set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; init; }

    /// <summary>
    /// 下载时间
    /// </summary>
    public DateTime? DownloadTime { get; private set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; private set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// 设置缩略图URI
    /// </summary>
    /// <param name="thumbnailUri">缩略图URI</param>
    /// <returns>文件附件</returns>
    public void SetThumbnail(Uri thumbnailUri)
    {
        ThumbnailUri = thumbnailUri;
    }

    /// <summary>
    /// 更新文件描述
    /// </summary>
    /// <param name="description">文件描述</param>
    /// <returns>文件附件</returns>
    public void UpdateDescription(string description)
    {
        Description = description;
    }

    /// <summary>
    /// 记录下载
    /// </summary>
    /// <returns>文件附件</returns>
    public void RecordDownload()
    {
        DownloadTime = DateTime.UtcNow;
        DownloadCount++;
    }

    /// <summary>
    /// 删除文件附件
    /// </summary>
    /// <returns>文件附件</returns>
    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("文件已删除");

        IsDeleted = true;
    }

    /// <summary>
    /// 获取格式化后的文件大小
    /// </summary>
    /// <returns>格式化后的文件大小</returns>
    public string GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = FileSize;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 是否为图片
    /// </summary>
    /// <returns>是否为图片</returns>
    public bool IsImage()
    {
        return FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 是否为视频
    /// </summary>
    /// <returns>是否为视频</returns>
    public bool IsVideo()
    {
        return FileType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 是否为音频
    /// </summary>
    /// <returns>是否为音频</returns>
    public bool IsAudio()
    {
        return FileType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 是否为文档
    /// </summary>
    /// <returns>是否为文档</returns>
    public bool IsDocument()
    {
        var documentTypes = new[]
            { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument", "text/" };
        return documentTypes.Any(t => FileType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 是否为其他文件
    /// </summary>
    /// <returns>是否为其他文件</returns>
    public bool IsOther()
    {
        return !IsImage() && !IsVideo() && !IsAudio() && !IsDocument();
    }
}