using FileDev.Domain.Entities;
using FileDev.Domain.Enum;

namespace FileDev.Domain.IServices;

/// <summary>
/// 内容附件弱引用管理服务（方案 C）：登记、注销与回收附件引用。
/// 负责维护 ContentRef 表，并返回「已无引用、可物理清理」的附件路径供上层执行存储清理。
/// </summary>
public interface IContentAttachmentService
{
    /// <summary>
    /// 登记一条附件引用。同一业务内容（contentId+contentType）重复引用同一文件时引用计数累加。
    /// </summary>
    Task RegisterAsync(string contentId, ContentType contentType, Uri fileUri, Guid sourceFileId,
        CancellationToken ct = default);

    /// <summary>
    /// 业务内容被删除时注销其下全部附件引用，返回因此不再被任何内容引用、可物理清理的附件 FileUri 列表。
    /// </summary>
    Task<IReadOnlyList<Uri>> UnregisterByContentAsync(string contentId, ContentType contentType,
        CancellationToken ct = default);

    /// <summary>
    /// 读取某业务内容登记的全部附件引用。
    /// </summary>
    Task<IReadOnlyList<ContentAttachmentRef>> GetByContentIdAsync(string contentId, ContentType contentType,
        CancellationToken ct = default);
}