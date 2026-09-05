namespace FileDev.Domain.Entities;

/// <summary>
/// 文件查询结果 DTO，由 NotFile 实体投影而来，创建后不可变。
/// </summary>
public class FileResult
{
    public Guid FileId { get; init; }
    public Guid UserId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public HashSet<string> FileTags { get; init; } = new();
    public string FileDescription { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public Uri FileUri { get; init; } = null!;
    public FileIdentity FileIdentity { get; init; }
    public DateTimeOffset UploadTime { get; init; }
    public DateTimeOffset UpdateTime { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeleteTime { get; init; }

    public static FileResult FromEntity(NotFile file)
    {
        return new FileResult
        {
            FileId = file.FileId,
            UserId = file.UserId,
            FileName = file.FileName,
            FileTags = [.. file.FileTags],
            FileDescription = file.FileDescription,
            FileSize = file.FileSize,
            FileUri = file.FileUri,
            FileIdentity = file.FileIdentity,
            UploadTime = file.UploadTime,
            UpdateTime = file.UpdateTime,
            IsDeleted = file.IsDeleted,
            DeleteTime = file.DeleteTime
        };
    }

    public string GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        // 使用 double 避免整数除法丢失精度
        var size = (double)FileSize;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    public bool IsPublic()
    {
        return FileIdentity == FileIdentity.FilePublic || FileIdentity == FileIdentity.FilePrivatePublic;
    }

    public bool IsPrivate()
    {
        return FileIdentity == FileIdentity.FilePrivate || FileIdentity == FileIdentity.FilePrivatePublic;
    }
}
