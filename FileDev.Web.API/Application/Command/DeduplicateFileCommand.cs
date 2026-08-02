namespace FileDev.Web.API.Application.Command;

public class DeduplicateFileCommand : IRequest<DeduplicateFileResponse>
{
    public string FileMd5 { get; set; } = null!;
    public long FileSize { get; set; }

    /// <summary>当前调用者（由服务端从 JWT 解析，用于秒传命中时绑定文件归属）</summary>
    public Guid UserId { get; set; }
}

public class DeduplicateFileResponse
{
    public bool Exists { get; set; }
    public Guid? FileId { get; set; }
    public string? FileUri { get; set; }
}
