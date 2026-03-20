namespace FileDev.Domain.Dto.Response;

public record NotFileStorageInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileMd5 { get; set; } = string.Empty;
    public string FileUri { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}