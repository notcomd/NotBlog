namespace Identity.Infrastructure.Repository;

public class NotClientRepository(IdentityDbContext dbContext) : INotClientRepository
{
    public IUnitOfWork UnitOfWork => dbContext;


    public async ValueTask<NotClient?> FindByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        return await dbContext.NotClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Status != ClientStatus.Revoked, ct);
    }

    public async ValueTask<NotClient?> FindByIdAsync(Guid notClientId, CancellationToken ct = default)
    {
        return await dbContext.NotClients.FindAsync([notClientId], ct);
    }

    public async Task<IReadOnlyList<NotClient>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.NotClients
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async ValueTask AddAsync(NotClient client, CancellationToken ct = default)
    {
        await dbContext.NotClients.AddAsync(client, ct);
    }

    public ValueTask UpdateAsync(NotClient client, CancellationToken ct = default)
    {
        dbContext.NotClients.Update(client);
        return ValueTask.CompletedTask;
    }


    
}
