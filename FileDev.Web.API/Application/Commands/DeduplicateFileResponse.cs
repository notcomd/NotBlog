namespace FileDev.Web.API.Application.Commands;

/// <summary>秒传检查响应 DTO</summary>
public class DeduplicateFileResponse
{
    public bool Exists { get; set; }
    public Guid? FileId { get; set; }
    public string? FileUri { get; set; }
}
