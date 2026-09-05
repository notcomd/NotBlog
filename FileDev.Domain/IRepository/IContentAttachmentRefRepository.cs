using Commons.SeedWork;
using FileDev.Domain.Entities;
using FileDev.Domain.Enum;

namespace FileDev.Domain.IRepository;

/// <summary>内容附件弱引用仓储：按业务内容 / 附件文件维度查询与维护引用计数。</summary>
public interface IContentAttachmentRefRepository : IRepository<ContentAttachmentRef, IUnitOfWork>
{
    /// <summary>读取某业务内容登记的全部附件引用。</summary>
    Task<IReadOnlyList<ContentAttachmentRef>> GetByContentIdAsync(string contentId, ContentType contentType,
        CancellationToken ct = default);

    /// <summary>读取引用某附件文件（FileUri）的全部引用，用于清点该文件是否仍被业务内容使用。</summary>
    Task<IReadOnlyList<ContentAttachmentRef>> GetByFileUriAsync(Uri fileUri, CancellationToken ct = default);

    /// <summary>统计某附件文件当前被业务内容引用的活跃点数（ActiveRefs 之和）。</summary>
    Task<int> CountActiveByFileUriAsync(Uri fileUri, CancellationToken ct = default);

    /// <summary>删除某业务内容下的全部引用行（内容删除时批量清理）。</summary>
    Task DeleteByContentAsync(string contentId, ContentType contentType, CancellationToken ct = default);

    /// <summary>追加一条引用。</summary>
    Task AddRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default);

    /// <summary>保存引用计数变更。</summary>
    Task UpdateRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default);

    /// <summary>删除引用行（引用计数归零后清理）。</summary>
    Task DeleteRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default);
}