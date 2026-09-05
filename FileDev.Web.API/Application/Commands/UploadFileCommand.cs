namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 小文件上传用例（gRPC UploadFile）。
/// 封装参数校验、配额检查、物理存储与元数据创建，事务由 TransactionBehavior 统一处理。
/// </summary>
public class UploadFileCommand : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public byte[] FileContent { get; set; } = null!;
    public HashSet<string>? FileTags { get; set; }
    public string? FileDescription { get; set; }
    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;

    /// <summary>预期文件哈希（SHA256），为空则跳过校验</summary>
    public string? ExpectedMd5 { get; set; }

    /// <summary>所属业务内容 ID（非空则本条为内容附件，登记 ContentRef）</summary>
    public string? ContentId { get; set; }

    /// <summary>业务内容类型（Post / Markdown / Video），仅在 ContentId 非空时生效</summary>
    public FileDev.Domain.Enum.ContentType ContentType { get; set; }
}
