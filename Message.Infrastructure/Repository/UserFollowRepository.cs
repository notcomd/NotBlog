
namespace Message.Infrastructure.Repository;

public class UserFollowRepository(MessageDbContext context) : IUserFollowRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserFollow> DbSet = context.UserFollows;

    public async Task<UserFollow?> GetAsync(Guid followerGuid, Guid followeeGuid)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.FollowerGuid == followerGuid && f.FolloweeGuid == followeeGuid);
    }

    public async Task<bool> ExistsAsync(Guid followerGuid, Guid followeeGuid)
    {
        return await DbSet.AnyAsync(f => f.FollowerGuid == followerGuid && f.FolloweeGuid == followeeGuid);
    }

    public async Task<IEnumerable<UserFollow>> GetFollowingAsync(Guid userGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(f => f.FollowerGuid == userGuid)
            .OrderByDescending(f => f.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserFollow>> GetFollowersAsync(Guid userGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(f => f.FolloweeGuid == userGuid)
            .OrderByDescending(f => f.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetFollowingIdsAsync(Guid userGuid)
    {
        return await DbSet
            .Where(f => f.FollowerGuid == userGuid)
            .Select(f => f.FolloweeGuid)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userGuid)
    {
        return await DbSet
            .Where(f => f.FolloweeGuid == userGuid)
            .Select(f => f.FollowerGuid)
            .ToListAsync();
    }

    public async Task<int> GetFollowingCountAsync(Guid userGuid)
    {
        return await DbSet.CountAsync(f => f.FollowerGuid == userGuid);
    }

    public async Task<int> GetFollowerCountAsync(Guid userGuid)
    {
        return await DbSet.CountAsync(f => f.FolloweeGuid == userGuid);
    }

    public async Task<UserFollow> AddAsync(UserFollow follow)
    {
        var entry = await DbSet.AddAsync(follow);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid followerGuid, Guid followeeGuid)
    {
        var follow = await GetAsync(followerGuid, followeeGuid);
        if (follow is not null)
            DbSet.Remove(follow);
    }
}
