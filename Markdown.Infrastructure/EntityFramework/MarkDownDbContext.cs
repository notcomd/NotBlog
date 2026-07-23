﻿using DomainInfrastructure;
using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;
using Markdown.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NotMediator;

namespace Markdown.Infrastructure.EntityFramework;

public class MarkDownDbContext(DbContextOptions<MarkDownDbContext> options, INotMediator notMediator)
    : DbContext(options), IUnitOfWork
{
    private readonly INotMediator _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));

    private IDbContextTransaction _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

    public DbSet<MarkDown> Markdowns { get; set; }


    /// <summary>
    ///     保存更改并分发领域事件
    /// </summary>
    public async Task<int> SavaChangesAsync(CancellationToken cancellationToken = default)
    {
        // 分发领域事件（顺序发布）
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     保存实体（事务性操作）
    /// </summary>
    public async Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default)
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
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction is not null) return null;
        _currentTransaction = await Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public sealed override int GetHashCode()
    {
        return base.GetHashCode();
    }


    public IDbContextTransaction GetContextTransaction()
    {
        return Database.BeginTransaction();
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