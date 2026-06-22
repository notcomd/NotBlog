using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public abstract class Repository<T>(MessageDbContext context)
    where T : class, IAggregateRoot
{
    protected readonly MessageDbContext Context = context ?? throw new ArgumentNullException(nameof(context));
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public IUnitOfWork UnitOfWork => Context;

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var entry = await DbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var entry = DbSet.Update(entity);
        return entry.Entity;
    }

    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    protected IQueryable<T> ApplyPaging(IQueryable<T> query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}