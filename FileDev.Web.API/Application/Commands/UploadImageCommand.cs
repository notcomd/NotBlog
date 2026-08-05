namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 图片上传用例（gRPC UploadImage）。
/// 封装格式验证（魔数校验+扩展名匹配）、配额检查、物理存储与元数据创建，
/// 事务由 TransactionBehavior 统一处理。
/// </summary>
public class UploadImageCommand : IRequest<UploadImageResult>
{
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public byte[] ImageContent { get; set; } = null!;
    public HashSet<string>? FileTags { get; set; }
    public string? FileDescription { get; set; }
    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;

    /// <summary>是否校验图片格式（魔数）与扩展名匹配</summary>
    public bool ValidateFormat { get; set; }
}
