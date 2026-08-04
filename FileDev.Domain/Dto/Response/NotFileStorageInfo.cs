namespace FileDev.Domain.Dto.Response;

public record NotFileStorageInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileMd5 { get; set; } = string.Empty;
    public string FileUri { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public FileType FileType { get; set; }
}