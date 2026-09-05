namespace FileDev.Domain.IRepository;

/// <summary>
/// 分片上传记录的文档仓储（MongoDB），提供分片生命周期的原子读写能力。
/// <para>
/// 方案 B 后不再继承 <see cref="Commons.SeedWork.IRepository{T,TUow}"/>：分片跟踪脱离 EF 工作单元，
/// 依赖 MongoDB <c>$addToSet</c> / <c>$set</c> 原子更新保证并发去重与状态一致。
/// </para>
/// </summary>
public interface IFileChunkRepository
{
    /// <summary>根据文件标识获取分片上传记录</summary>
    Task<FileChunkRecord?> GetByFileKeyAsync(string fileKey, CancellationToken ct = default);

    /// <summary>幂等初始化：以 fileKey 为唯一键插入文档，已存在则忽略冲突（重试/并发初始化安全）</summary>
    Task InsertAsync(FileChunkRecord record, CancellationToken ct = default);

    /// <summary>原子追加已上传分片索引（$addToSet 去重），并将状态从 Pending 提升为 Uploading</summary>
    Task MarkChunkUploadedAsync(string fileKey, int chunkIndex, CancellationToken ct = default);

    /// <summary>原子标记合并完成（$set Status=Merged, CompletedAt=UTC now）</summary>
    Task MarkMergedAsync(string fileKey, CancellationToken ct = default);

    /// <summary>原子标记取消（$set Status=Cancelled, CompletedAt=UTC now）</summary>
    Task MarkCancelledAsync(string fileKey, CancellationToken ct = default);

    /// <summary>删除分片上传记录</summary>
    Task DeleteAsync(string fileKey, CancellationToken ct = default);

    /// <summary>获取过期的分片记录（用于后台清理，Status 非 Merged/Cancelled 且 CreatedAt 早于阈值）</summary>
    Task<IEnumerable<FileChunkRecord>> GetExpiredRecordsAsync(DateTimeOffset threshold,
        CancellationToken ct = default);
}