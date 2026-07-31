namespace FileDev.Domain.IRepository;

public interface IFileChunkRepository : IRepository<FileChunkRecord>
{
    /// <summary>根据文件标识获取分片上传记录</summary>
    Task<FileChunkRecord?> GetByFileKeyAsync(string fileKey, CancellationToken ct = default);

    /// <summary>插入新的分片上传记录</summary>
    Task InsertAsync(FileChunkRecord record, CancellationToken ct = default);

    /// <summary>更新分片上传记录</summary>
    Task UpdateAsync(FileChunkRecord record, CancellationToken ct = default);

    /// <summary>删除分片上传记录</summary>
    Task DeleteAsync(string fileKey, CancellationToken ct = default);

    /// <summary>获取过期的分片记录（用于后台清理）</summary>
    Task<IEnumerable<FileChunkRecord>> GetExpiredRecordsAsync(DateTimeOffset threshold,
        CancellationToken ct = default);
}
