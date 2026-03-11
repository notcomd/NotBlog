namespace FileDev.Domain.Entities;

public class FileResult
{
    public Guid FileId { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public HashSet<string> FileTags { get; set; } = new();
    public string FileDescription { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public double FileSize { get; set; }
    public Uri FileUri { get; set; } = null!;
    public FileIdentity FileIdentity { get; set; }
    public DateTime UploadTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeleteTime { get; set; }

    public static FileResult FromEntity(NotFile file)
    {
        return new FileResult
        {
            FileId = file.FileId,
            UserId = file.UserId,
            FileName = file.FileName,
            FileTags = new HashSet<string>(file.FileTags),
            FileDescription = file.FileDescription,
            FileType = file.FileType,
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
        int order = 0;
        double size = FileSize;
        
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
