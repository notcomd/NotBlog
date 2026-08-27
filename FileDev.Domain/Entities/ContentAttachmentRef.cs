using FileDev.Domain.Enum;

namespace FileDev.Domain.Entities;

/// <summary>
/// 内容附件弱引用（方案 C：Source 列 + ContentRef 表）。
/// 记录「业务内容（发帖 / Markdown / 视频）」对某个附件文件（NotFile，Source=ContentAttachment）的引用。
/// 用于精确判定附件能否物理回收：仅当没有业务内容继续引用（ActiveRefs==0）且未存续全新引用时才清理。
/// </summary>
public class ContentAttachmentRef : Entity<Guid>, IAggregateRoot
{
    /// <summary>引用方业务内容 ID（如帖子/文档/视频的聚合根 Id）。</summary>
    public string ContentId { get; private set; } = string.Empty;

    /// <summary>业务内容类型（Post / Markdown / Video）。</summary>
    public ContentType ContentType { get; private set; }

    /// <summary>被引用的附件物理路径（与 NotFile.FileUri 对齐）。</summary>
    public Uri FileUri { get; private set; } = null!;

    /// <summary>被引用的附件文件记录 Id（NotFile.FileId），便于回查与按文件聚合。</summary>
    public Guid SourceFileId { get; private set; }

    /// <summary>该业务内容当前持有的引用计数（同一内容多次引用同一附件文件时累加）。</summary>
    public int ActiveRefs { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>引用计数 +1（同一业务内容对同一文件重复登记时累加）。</summary>
    public void Increment()
    {
        ActiveRefs += 1;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private ContentAttachmentRef()
    {
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static ContentAttachmentRef Create(string contentId, ContentType contentType, Uri fileUri,
        Guid sourceFileId, int activeRefs = 1)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("内容 ID 不能为空", nameof(contentId));
        ArgumentNullException.ThrowIfNull(fileUri);
        if (sourceFileId == Guid.Empty)
            throw new ArgumentException("附件文件 ID 不能为空", nameof(sourceFileId));
        if (activeRefs < 1)
            throw new ArgumentOutOfRangeException(nameof(activeRefs), "初始引用数至少为 1");

        return new ContentAttachmentRef
        {
            ContentId = contentId.Trim(),
            ContentType = contentType,
            FileUri = fileUri,
            SourceFileId = sourceFileId,
            ActiveRefs = activeRefs
        };
    }
}