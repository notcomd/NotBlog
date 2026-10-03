using Commons.Extensions;
using Microsoft.EntityFrameworkCore.Storage;
using NotMediator.Abstractions;
using Commons.SeedWork;

namespace Video.Infrastructure.EntityFramework;

/// <summary>
/// Video 服务数据库上下文（EF Core）：承载视频 / 收藏夹 / 弹幕 / 评论 / 观看历史五类聚合，
/// 并在保存时统一分发领域事件（IUnitOfWork 实现，供仓储提交）。
/// </summary>
public class VideoDbContext(DbContextOptions<VideoDbContext> options, INotMediator notMediator)
    : DbContext(options), IUnitOfWork
{
    private readonly INotMediator _notMediator =
        notMediator ?? throw new ArgumentNullException(nameof(notMediator), "Mediator cannot be null");


    private IDbContextTransaction? _currentTransaction;

    /// <summary>视频聚合集合</summary>
    public DbSet<Videos> Videos { get; set; }

    /// <summary>视频收藏夹集合</summary>
    public DbSet<VideoCollection> VideoCollections { get; set; }

    /// <summary>视频弹幕集合</summary>
    public DbSet<VideoBarrage> VideoBarrages { get; set; }

    /// <summary>视频评论集合</summary>
    public DbSet<VideoReview> VideoReviews { get; set; }

    /// <summary>观看历史集合</summary>
    public DbSet<VideoHistory> VideoHistories { get; set; }

    /// <summary>当前是否存在未提交的事务。</summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>保存更改并分发领域事件，返回受影响行数。</summary>
    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>保存实体并分发领域事件，返回是否至少写入一行。</summary>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken) > 0;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    /// <summary>开启事务；已存在活动事务时直接返回。</summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
            return _currentTransaction;
        _currentTransaction = await Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    /// <summary>提交事务（先保存更改再提交），失败时回滚并抛出。</summary>
    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        if (transaction is null) throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null");
        if (transaction != _currentTransaction)
            throw new InvalidOperationException("The provided transaction does not match the current transaction.");
        try
        {
            await base.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction!.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>回滚当前事务并释放事务对象。</summary>
    public void RollbackTransaction(IDbContextTransaction transaction)
    {
        try
        {
            _currentTransaction?.Rollback();
        }

        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction!.Dispose();
                _currentTransaction = null;
            }
        }
    }
}