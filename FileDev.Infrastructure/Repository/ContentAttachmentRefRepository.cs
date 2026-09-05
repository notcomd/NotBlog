using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

/// <summary>内容附件弱引用仓储：持久化 ContentRef 表。</summary>
public class ContentAttachmentRefRepository(NotFileDbContext notFileDbContext)
    : IContentAttachmentRefRepository
{
    private readonly NotFileDbContext _db = notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;

    public async Task<IReadOnlyList<ContentAttachmentRef>> GetByContentIdAsync(string contentId,
        ContentType contentType, CancellationToken ct = default)
    {
        return await _db.ContentAttachmentRefs.AsNoTracking()
            .Where(x => x.ContentId == contentId && x.ContentType == contentType)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ContentAttachmentRef>> GetByFileUriAsync(Uri fileUri,
        CancellationToken ct = default)
    {
        return await _db.ContentAttachmentRefs.AsNoTracking()
            .Where(x => x.FileUri == fileUri)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> CountActiveByFileUriAsync(Uri fileUri, CancellationToken ct = default)
    {
        return await _db.ContentAttachmentRefs.AsNoTracking()
            .Where(x => x.FileUri == fileUri)
            .SumAsync(x => (int?)x.ActiveRefs, ct).ConfigureAwait(false) ?? 0;
    }

    public async Task DeleteByContentAsync(string contentId, ContentType contentType, CancellationToken ct = default)
    {
        var rows = await _db.ContentAttachmentRefs
            .Where(x => x.ContentId == contentId && x.ContentType == contentType)
            .ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count > 0)
            _db.ContentAttachmentRefs.RemoveRange(rows);
    }

    public async Task AddRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default)
    {
        await _db.ContentAttachmentRefs.AddAsync(attachmentRef, ct).ConfigureAwait(false);
    }

    public Task UpdateRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default)
    {
        _db.ContentAttachmentRefs.Update(attachmentRef);
        return Task.CompletedTask;
    }

    public async Task DeleteRefAsync(ContentAttachmentRef attachmentRef, CancellationToken ct = default)
    {
        var tracked = await _db.ContentAttachmentRefs
            .FirstOrDefaultAsync(x => x.Id == attachmentRef.Id, ct).ConfigureAwait(false);
        if (tracked is not null)
            _db.ContentAttachmentRefs.Remove(tracked);
    }
}