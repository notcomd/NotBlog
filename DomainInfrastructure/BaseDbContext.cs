using Microsoft.EntityFrameworkCore;

namespace DomainInfrastructure;

/// <summary>
/// EF Core 基础 DbContext
/// 集成领域事件发布机制，在 SaveChangesAsync 时自动派发领域事件
/// </summary>
public abstract class BaseDbContext : DbContext
{
    /// <summary>
    /// 领域事件分发器（由子类或 DI 注入）
    /// </summary>
    private readonly Func<BaseDbContext, ValueTask>? _domainEventDispatcher;

    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    protected BaseDbContext(
        DbContextOptions options,
        Func<BaseDbContext, ValueTask> domainEventDispatcher) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>禁止同步 SaveChanges，强制使用异步方法</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new InvalidOperationException("请使用 SaveChangesAsync 方法代替同步 SaveChanges。");
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        if (_domainEventDispatcher != null)
            await _domainEventDispatcher(this);

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}