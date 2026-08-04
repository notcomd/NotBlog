using Video.Domain.Entities;
using Commons.SeedWork;

namespace Video.Domain.IRepository;

/// <summary>
/// 视频观看历史仓储接口 — 支持分页查询、批量删除和统计分析。
/// 针对海量历史数据场景进行了查询优化设计。
/// </summary>
public interface IVideoHistoryRepository : IRepository<VideoHistory, IUnitOfWork>
{
    // ── 单个查询 ──

    /// <summary>按 GUID 查找历史记录。</summary>
    Task<VideoHistory?> FindByIdAsync(Guid videoHistoryGuid);

    // ── 用户历史（分页，防止内存溢出） ──

    /// <summary>获取用户观看历史，按观看时间倒序分页。</summary>
    Task<List<VideoHistory>> FindByUserAsync(Guid userGuid, int page, int pageSize);

    /// <summary>获取用户对特定视频的观看历史（多次观看）。</summary>
    Task<List<VideoHistory>> FindByUserAndVideoAsync(Guid userGuid, Guid videoGuid);

    /// <summary>获取用户对特定视频的最后一次观看记录。</summary>
    Task<VideoHistory?> FindLastByUserAndVideoAsync(Guid userGuid, Guid videoGuid);

    // ── 视频维度查询 ──

    /// <summary>获取视频的观看历史，按时间倒序分页。</summary>
    Task<List<VideoHistory>> FindByVideoAsync(Guid videoGuid, int page, int pageSize);

    /// <summary>获取视频的总观看次数。</summary>
    Task<long> CountByVideoAsync(Guid videoGuid);

    // ── 统计分析 ──

    /// <summary>获取视频的观看完成率（IsCompleted / total）。</summary>
    Task<double> GetCompletionRateAsync(Guid videoGuid);

    /// <summary>获取视频的平均观看时长。</summary>
    Task<TimeSpan> GetAverageDurationAsync(Guid videoGuid);

    // ── 写入操作 ──

    Task AddAsync(VideoHistory history);
    Task UpdateAsync(VideoHistory history);

    // ── 删除操作 ──

    /// <summary>按 GUID 删除单条记录。</summary>
    Task DeleteByIdAsync(Guid videoHistoryGuid);

    /// <summary>删除用户对特定视频的所有历史记录。</summary>
    Task DeleteByUserAndVideoAsync(Guid userGuid, Guid videoGuid);

    /// <summary>清理 N 天前的历史数据（批量）。</summary>
    Task<int> CleanupOlderThanAsync(DateTimeOffset threshold);
}
