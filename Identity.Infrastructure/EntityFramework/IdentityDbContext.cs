using Identity.Infrastructure.Idempotent;
using Notcomd.EventBus.Outbox;

namespace Identity.Infrastructure.EntityFramework;

public class IdentityDbContext : DbContext, IUnitOfWork
{
    private readonly INotMediator _notMediator;

    private IDbContextTransaction? _currentTransaction;

    // public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, INotMediator mediator) : base(options)
    {
        _notMediator = mediator ?? throw new ArgumentNullException(nameof(mediator), "Mediator cannot be null");
        Debug.WriteLine($"IdentityDbContext::Context->{GetHashCode()}");
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Roles> Roles { get; set; }

    public DbSet<NotClient> NotClients { get; set; }

    public DbSet<RoleGroup> RoleGroups { get; set; }

    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<ClientRequest> ClientRequests{get;set;}

    public DbSet<UserLoginHistory> UserLoginHistories { get; set; }

    public bool HasActiveTransaction => _currentTransaction != null;

    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        //if (_notMediator is null) throw new ArgumentNullException(nameof(_notMediator), "Mediator cannot be null");
        await _notMediator.DispatchDomainEventsAsync(this);
        // Q-02：返回真实影响行数（此前恒返回 0）
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this);
        _ = await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    public sealed override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public IDbContextTransaction? GetContextTransaction() => _currentTransaction;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //modelBuilder.HasDefaultSchema("identity");

        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleGroupEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserAccessFailEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserSafetyEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NotClientEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserExternalLoginEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ClientRequestTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionEntityTypeConfigurtion());
        modelBuilder.ApplyConfiguration(new UserLoginHistoryEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageTypeConfiguration());
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return _currentTransaction;
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
