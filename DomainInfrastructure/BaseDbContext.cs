
using DomainCommonst;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using NotMediator;

namespace Notcomd.DomainCommand;

public abstract class BaseDbContext<TDbContext> : DbContext, IUnitOfWork where TDbContext : DbContext

{

    private readonly INotMediator _mediator;

    private IDbContextTransaction dbContextTransaction;

    public BaseDbContext(DbContextOptions<TDbContext> options, INotMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public async Task<int> SavaChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_mediator != null)
        {
            await _mediator.DispatchDomainEventsAsync(this);
        }
        var result = await base.SaveChangesAsync(cancellationToken);
        return 0;
    }
    public async Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default)
    {
        if (_mediator != null)
        {
            await _mediator.DispatchDomainEventsAsync(this);
        }
        var result = await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    public IDbContextTransaction BeginTransaction()
    {
        if (dbContextTransaction == null)
        {
            dbContextTransaction = Database.BeginTransaction();
        }
        return dbContextTransaction;
    }

    public IDbContextTransaction? GetCurrentTransaction()
    {
        return dbContextTransaction;
    }

    public void RollbackTransaction()
    {
        try
        {
            dbContextTransaction?.Rollback();
        }
        finally
        {
            if (dbContextTransaction != null)
            {
                dbContextTransaction.Dispose();
                dbContextTransaction = null;
            }
        }
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            dbContextTransaction?.Commit();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (dbContextTransaction != null)
            {
                await dbContextTransaction.DisposeAsync();
                dbContextTransaction = null;
            }
        }
    }


}