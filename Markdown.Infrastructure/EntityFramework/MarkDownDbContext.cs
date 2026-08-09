using Commons.Extensions;
using Commons.SeedWork;
using Markdown.Infrastructure.Configuration;
using Markdown.Infrastructure.Idempotent;
using Microsoft.EntityFrameworkCore.Storage;
using NotMediator;

namespace Markdown.Infrastructure.EntityFramework;

public class MarkDownDbContext(DbContextOptions<MarkDownDbContext> options, INotMediator notMediator)
    : DbContext(options), IUnitOfWork
{
    private readonly INotMediator _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));

    private IDbContextTransaction _currentTransaction = null!;

    public bool HasActiveTransaction => _currentTransaction != null;

    public DbSet<MarkDown> Markdowns { get; set; }

    public DbSet<ClientRequest> ClientRequests { get; set; }

    public DbSet<MarkReviewLike> MarkReviewLikes { get; set; }

    public DbSet<MarkFavorite> MarkFavorites { get; set; }

    public DbSet<MarkFavoriteTag> MarkFavoriteTags { get; set; }


    /// <summary>
    ///     保存更改并分发领域事件
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 分发领域事件（顺序发布）
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     保存实体（事务性操作）
    /// </summary>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this);
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
    // 注意：MarkReview 作为子聚合，不直接暴露 DbSet，通过 MarkDown.MarkReview 访问

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new MarkDownEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarkReviewEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewImageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarkFavoriteEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarkFavoriteTagEntityConfiguration());

        // ClientRequest 幂等性记录表配置
        modelBuilder.Entity<ClientRequest>(entity =>
        {
            entity.ToTable("ClientRequest");
            entity.HasKey(e => e.ClientRequestId);
            entity.Property(e => e.ClientRequestName).HasMaxLength(256).IsRequired();
        });

        // MarkReviewLike 评论点赞记录表配置（唯一约束实现点赞去重）
        modelBuilder.Entity<MarkReviewLike>(entity =>
        {
            entity.ToTable("MarkReviewLike");
            entity.Property(x => x.Id).UseHiLo("MarkReviewLikeGuid");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MarkReviewGuid).IsRequired();
            entity.Property(x => x.UserId).IsRequired();
            entity.HasIndex(x => new { x.MarkReviewGuid, x.UserId }).IsUnique();
            entity.HasIndex(x => x.MarkReviewGuid);
        });
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return _currentTransaction;
        _currentTransaction = await Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public sealed override int GetHashCode()
    {
        return base.GetHashCode();
    }


    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        if (transaction is null) throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null");
        if (transaction != _currentTransaction)
            throw new InvalidOperationException("The provided transaction does not match the current transaction.");
        try
        {
            await SaveChangesAsync();
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
                _currentTransaction.Dispose();
                _currentTransaction = null!;
            }
        }
    }

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
                _currentTransaction.Dispose();
                _currentTransaction = null!;
            }
        }
    }
}