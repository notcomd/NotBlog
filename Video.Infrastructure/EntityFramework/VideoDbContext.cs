using Commons.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NotMediator;
using Video.Domain.Entities;
using Commons.SeedWork;

namespace Video.Infrastructure.EntityFramework;

public class VideoDbContext(DbContextOptions<VideoDbContext> options, INotMediator notMediator)
    : DbContext(options), IUnitOfWork
{
    private readonly INotMediator _notMediator =
        notMediator ?? throw new ArgumentNullException(nameof(notMediator), "Mediator cannot be null");


    private IDbContextTransaction? _currentTransaction;

    public DbSet<Videos> Videos { get; set; }

    public DbSet<VideoCollection> VideoCollections { get; set; }

    public DbSet<VideoBarrage> VideoBarrages { get; set; }

    public DbSet<VideoReview> VideoReviews { get; set; }

    public DbSet<VideoHistory> VideoHistories { get; set; }

    public bool HasActiveTransaction => _currentTransaction != null;

    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this);
        return await base.SaveChangesAsync(cancellationToken) > 0;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
            return _currentTransaction;
        _currentTransaction = await Database.BeginTransactionAsync();
        return _currentTransaction;
    }

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