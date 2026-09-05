using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructure.Mongo;
using MongoDB.Driver;

namespace FileDev.Infrastructure.Repository;

/// <summary>
/// 分片上传记录的 MongoDB 文档仓储。
/// <para>
/// 替代原 EF 仓储：分片跟踪脱离关系型工作单元，依赖 MongoDB 的原子更新（<c>$addToSet</c> / <c>$set</c>）
/// 保证并发分片去重与状态一致性。所有写操作即时生效，无 SaveChanges 语义。
/// </para>
/// </summary>
public class MongoFileChunkRepository : IFileChunkRepository
{
    private readonly IMongoCollection<FileChunkRecord> _files;

    public MongoFileChunkRepository(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _files = MongoChunkCollection.Get(database);
    }

    public async Task<FileChunkRecord?> GetByFileKeyAsync(string fileKey, CancellationToken ct = default)
    {
        return await _files.Find(Builders<FileChunkRecord>.Filter.Eq(r => r.FileKey, fileKey))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task InsertAsync(FileChunkRecord record, CancellationToken ct = default)
    {
        try
        {
            await _files.InsertOneAsync(record, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // 幂等初始化：FileKey 唯一约束兜底，重试/并发初始化时静默忽略已存在记录。
        }
    }

    public async Task MarkChunkUploadedAsync(string fileKey, int chunkIndex, CancellationToken ct = default)
    {
        // 过滤状态仅为 Pending/Uploading：已合并或已取消的记录不接受新分片，避免复活。
        var filter = Builders<FileChunkRecord>.Filter.Eq(r => r.FileKey, fileKey)
                     & Builders<FileChunkRecord>.Filter.In(r => r.Status,
                         new[] { ChunkUploadStatus.Pending, ChunkUploadStatus.Uploading });
        // $addToSet 原子防重（跨实例安全），并将状态从 Pending 提升为 Uploading。
        var update = Builders<FileChunkRecord>.Update
            .AddToSet(r => r.UploadedChunks, chunkIndex)
            .Set(r => r.Status, ChunkUploadStatus.Uploading);

        await _files.UpdateOneAsync(filter, update, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task MarkMergedAsync(string fileKey, CancellationToken ct = default)
    {
        var filter = Builders<FileChunkRecord>.Filter.Eq(r => r.FileKey, fileKey);
        var update = Builders<FileChunkRecord>.Update
            .Set(r => r.Status, ChunkUploadStatus.Merged)
            .Set(r => r.CompletedAt, DateTimeOffset.UtcNow);

        await _files.UpdateOneAsync(filter, update, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task MarkCancelledAsync(string fileKey, CancellationToken ct = default)
    {
        var filter = Builders<FileChunkRecord>.Filter.Eq(r => r.FileKey, fileKey);
        var update = Builders<FileChunkRecord>.Update
            .Set(r => r.Status, ChunkUploadStatus.Cancelled)
            .Set(r => r.CompletedAt, DateTimeOffset.UtcNow);

        await _files.UpdateOneAsync(filter, update, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        await _files.DeleteOneAsync(
            Builders<FileChunkRecord>.Filter.Eq(r => r.FileKey, fileKey),
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FileChunkRecord>> GetExpiredRecordsAsync(DateTimeOffset threshold,
        CancellationToken ct = default)
    {
        var filter = Builders<FileChunkRecord>.Filter.Nin(r => r.Status,
                         new[] { ChunkUploadStatus.Merged, ChunkUploadStatus.Cancelled })
                     & Builders<FileChunkRecord>.Filter.Lt(r => r.CreatedAt, threshold);

        return await _files.Find(filter)
            .Limit(1000)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}