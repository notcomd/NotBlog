using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 内容附件弱引用管理服务实现：维护 ContentRef 表。
/// 注销时先删除该业务内容全部引用行，再按文件统计剩余活跃引用；
/// 仅当某附件文件的活跃引用归零时，才将其 FileUri 标记为「可物理清理」交上层处理。
/// </summary>
public class ContentAttachmentService(IContentAttachmentRefRepository refRepository)
    : IContentAttachmentService
{
    private readonly IContentAttachmentRefRepository _refRepository = refRepository
        ?? throw new ArgumentNullException(nameof(refRepository));

    public async Task RegisterAsync(string contentId, ContentType contentType, Uri fileUri, Guid sourceFileId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("内容 ID 不能为空", nameof(contentId));
        ArgumentNullException.ThrowIfNull(fileUri);
        if (sourceFileId == Guid.Empty)
            throw new ArgumentException("附件文件 ID 不能为空", nameof(sourceFileId));

        var existing = await _refRepository.GetByContentIdAsync(contentId, contentType, ct).ConfigureAwait(false)
            is { Count: > 0 } refs
                ? refs.FirstOrDefault(r => r.FileUri == fileUri)
                : null;

        if (existing is null)
        {
            await _refRepository.AddRefAsync(
                ContentAttachmentRef.Create(contentId, contentType, fileUri, sourceFileId), ct).ConfigureAwait(false);
        }
        else
        {
            existing.Increment();
            await _refRepository.UpdateRefAsync(existing, ct).ConfigureAwait(false);
        }

        await _refRepository.UnitOfWork.SaveEntitiesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Uri>> UnregisterByContentAsync(string contentId, ContentType contentType,
        CancellationToken ct = default)
    {
        var refs = await _refRepository.GetByContentIdAsync(contentId, contentType, ct).ConfigureAwait(false);
        if (refs.Count == 0)
            return [];

        var affectedFiles = refs.Select(r => r.FileUri).Distinct().ToList();

        // 先删除该业务内容的全部引用行
        await _refRepository.DeleteByContentAsync(contentId, contentType, ct).ConfigureAwait(false);
        await _refRepository.UnitOfWork.SaveEntitiesAsync(ct).ConfigureAwait(false);

        // 再对受影响文件统计剩余活跃引用，归零才标记可清理
        var candidates = new List<Uri>();
        foreach (var fileUri in affectedFiles)
        {
            var remaining = await _refRepository.CountActiveByFileUriAsync(fileUri, ct).ConfigureAwait(false);
            if (remaining <= 0)
                candidates.Add(fileUri);
        }

        return candidates;
    }

    public async Task<IReadOnlyList<ContentAttachmentRef>> GetByContentIdAsync(string contentId,
        ContentType contentType, CancellationToken ct = default)
    {
        return await _refRepository.GetByContentIdAsync(contentId, contentType, ct).ConfigureAwait(false);
    }
}