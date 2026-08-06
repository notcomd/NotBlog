
namespace Message.Infrastructure.Repository;

public class CircleInvitationRepository(MessageDbContext context) : ICircleInvitationRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<CircleInvitation> DbSet = context.CircleInvitations;

    public async Task<CircleInvitation?> GetByIdAsync(Guid inviteGuid)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.InviteGuid == inviteGuid);
    }

    public async Task<CircleInvitation?> GetByCodeAsync(string code)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.Code == code);
    }

    public async Task<CircleInvitation?> GetByTokenAsync(Guid token)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<IEnumerable<CircleInvitation>> GetByCircleAsync(Guid circleGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(i => i.CircleGuid == circleGuid)
            .OrderByDescending(i => i.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByCircleAsync(Guid circleGuid)
    {
        return await DbSet.CountAsync(i => i.CircleGuid == circleGuid);
    }

    public async Task<IEnumerable<CircleInvitation>> GetByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet.Where(i => i.InviteeGuid == inviteeGuid);
        if (pendingOnly)
            query = query.Where(i => i.Status == CircleInvitationStatus.Pending);

        return await query
            .OrderByDescending(i => i.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true)
    {
        var query = DbSet.Where(i => i.InviteeGuid == inviteeGuid);
        if (pendingOnly)
            query = query.Where(i => i.Status == CircleInvitationStatus.Pending);
        return await query.CountAsync();
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await DbSet.AnyAsync(i => i.Code == code);
    }

    public async Task<CircleInvitation> AddAsync(CircleInvitation invitation)
    {
        var entry = await DbSet.AddAsync(invitation);
        return entry.Entity;
    }

    public async Task<CircleInvitation> UpdateAsync(CircleInvitation invitation)
    {
        var entry = DbSet.Update(invitation);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid inviteGuid)
    {
        var invitation = await GetByIdAsync(inviteGuid);
        if (invitation is not null)
            DbSet.Remove(invitation);
    }
}
