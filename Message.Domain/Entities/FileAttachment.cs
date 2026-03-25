using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

/// <summary>
///  文件附件
/// </summary>
public class FileAttachment : Entity
{
    public FileAttachment(Guid messageId, string fileName, string fileType, long fileSize, Uri fileUri)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");

        AttachmentId = Guid.NewGuid();
        MessageId = messageId;
        FileName = fileName;
        FileType = fileType;
        FileSize = fileSize;
        FileUri = fileUri;
        UploadTime = DateTime.UtcNow;
        DownloadCount = 0;
        IsDeleted = false;
    }

    private FileAttachment()
    {
        AttachmentId = Guid.NewGuid();
        UploadTime = DateTime.UtcNow;
        DownloadCount = 0;
        IsDeleted = false;
    }

    public Guid AttachmentId { get; init; }
    public Guid MessageId { get; init; }
    public string FileName { get; private set; }
    public string FileType { get; private set; }
    public long FileSize { get; private set; }
    public Uri FileUri { get; private set; }
    public Uri? ThumbnailUri { get; private set; }
    public string? MimeType { get; private set; }
    public string? Description { get; set; }
    public DateTime UploadTime { get; init; }
    public DateTime? DownloadTime { get; private set; }
    public int DownloadCount { get; private set; }
    public bool IsDeleted { get; private set; }

    public void SetThumbnail(Uri thumbnailUri)
    {
        ThumbnailUri = thumbnailUri;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }

    public void RecordDownload()
    {
        DownloadTime = DateTime.UtcNow;
        DownloadCount++;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("文件已删除");

        IsDeleted = true;
    }

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

    public bool IsImage()
    {
        return FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsVideo()
    {
        return FileType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAudio()
    {
        return FileType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDocument()
    {
        var documentTypes = new[]
            { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument", "text/" };
        return documentTypes.Any(t => FileType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }
}