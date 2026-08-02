using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Notcomd.EventBus.Outbox;

/// <summary>
/// 基于 EF Core 的 Outbox 消息存储实现
/// </summary>
public class EfCoreOutboxStore<TDbContext> : IOutboxStore where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<EfCoreOutboxStore<TDbContext>>? _logger;

    // S-19：防并发扫描——同一进程内禁止两个扫描周期同时取消息
    private static readonly SemaphoreSlim ScanLock = new(1, 1);

    public EfCoreOutboxStore(TDbContext dbContext, ILogger<EfCoreOutboxStore<TDbContext>>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OutboxMessage>().AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<OutboxMessage>> GetPendingBatchAsync(int batchSize,
        CancellationToken cancellationToken = default)
    {
        await ScanLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // S-19：取 Pending 消息 + 已到重试时间的 Failed 消息（指数退避）
            var now = DateTimeOffset.UtcNow;
            return await _dbContext.Set<OutboxMessage>()
                .Where(m => m.Status == OutboxStatus.Pending
                            || (m.Status == OutboxStatus.Failed && m.NextRetryAt <= now))
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ScanLock.Release();
        }
    }

    public async Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message != null)
        {
            message.MarkSent();
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message != null)
        {
            message.IncrementFailure(error);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task CleanupExpiredAsync(int retentionDays,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var expiredMessages = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxStatus.Sent && m.SentAt < cutoffDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredMessages.Count > 0)
        {
            _dbContext.Set<OutboxMessage>().RemoveRange(expiredMessages);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("[Evenbus-Outbox] 清理过期消息: {Count} 条", expiredMessages.Count);
        }
    }
}