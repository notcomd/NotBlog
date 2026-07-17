namespace FileDev.Infrastructure.EntityFramework;

public class NotFileDbContext(DbContextOptions<NotFileDbContext> options, INotMediator mediator)
    : DbContext(options), IUnitOfWork
{
    private readonly INotMediator _notMediator = mediator ??
                                                 throw new ArgumentNullException(nameof(mediator),
                                                     "Mediator cannot be null");

    private IDbContextTransaction _currentTransaction;


    public DbSet<NotFile> NotFiles { get; set; }

    public DbSet<ClientRequest> ClientRequests { get; set; }

    public DbSet<NotFileGroup> NotFileGroups { get; set; }

    public DbSet<FileChunkRecord> FileChunkRecords { get; set; }

    public bool HasActiveTransaction => _currentTransaction is not null;

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

    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;


    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new NotFileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NotFileGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileChunkRecordEntityConfig());
        modelBuilder.ApplyConfiguration(new ClientRequestTypeConfiguration());
    }
}