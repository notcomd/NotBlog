using System.Diagnostics;
using Identity.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore.Storage;

namespace Identity.Infrastructure.EntityFramework;

public class IdentityDbContext : DbContext, IUnitOfWork
{
    private readonly INotMediator _notMediator;

    private IDbContextTransaction _currentTransaction;

    // public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, INotMediator mediator) : base(options)
    {
        _notMediator = mediator ?? throw new ArgumentNullException(nameof(mediator), "Mediator cannot be null");
        Debug.WriteLine($"IdentityDbContext::Context->{GetHashCode()}");
    }

    public DbSet<User> Users { get; set; }

    public DbSet<UserSafety> userSafeties { get; set; }

    public DbSet<UserAccessFail> UserAccessFails { get; set; }

    public DbSet<Roles> Roles { get; set; }

    public DbSet<NotClient> NotClients { get; set; }

    public bool HasActiveTransaction => _currentTransaction != null;

    public async Task<int> SavaChangesAsync(CancellationToken cancellationToken = default)
    {
        //if (_notMediator is null) throw new ArgumentNullException(nameof(_notMediator), "Mediator cannot be null");
        await _notMediator.DispatchDomainEventsAsync(this);
        _ = await base.SaveChangesAsync(cancellationToken);
        return 0;
    }

    public async Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this);
        _ = await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    public IDbContextTransaction GetContextTransaction()
    {
        return _currentTransaction;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Identity");

        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new Author2EntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserAccessFailEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserSafetyEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NotClientEntityTypeConfiguration());
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return null;
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
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
#nullable enable