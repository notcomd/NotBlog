
namespace Message.Infrastructure.Repository;

/// <summary>用户关注关系仓储实现，负责 UserFollows 表的查询与持久化。</summary>
public class UserFollowRepository(MessageDbContext context) : IUserFollowRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserFollow> DbSet = context.UserFollows;

    /// <summary>获取指定的关注关系，不存在时返回 null。</summary>
    public async Task<UserFollow?> GetAsync(Guid followerGuid, Guid followeeGuid)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.FollowerGuid == followerGuid && f.FolloweeGuid == followeeGuid);
    }

    /// <summary>判断指定的关注关系是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid followerGuid, Guid followeeGuid)
    {
        return await DbSet.AnyAsync(f => f.FollowerGuid == followerGuid && f.FolloweeGuid == followeeGuid);
    }

    /// <summary>分页获取指定用户关注的人，按创建时间倒序。</summary>
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

    /// <summary>分页获取指定用户的粉丝，按创建时间倒序。</summary>
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

    /// <summary>获取指定用户关注的所有用户 ID。</summary>
    public async Task<IEnumerable<Guid>> GetFollowingIdsAsync(Guid userGuid)
    {
        return await DbSet
            .Where(f => f.FollowerGuid == userGuid)
            .Select(f => f.FolloweeGuid)
            .ToListAsync();
    }

    /// <summary>获取关注指定用户的所有粉丝 ID。</summary>
    public async Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userGuid)
    {
        return await DbSet
            .Where(f => f.FolloweeGuid == userGuid)
            .Select(f => f.FollowerGuid)
            .ToListAsync();
    }

    /// <summary>统计指定用户关注的人数。</summary>
    public async Task<int> GetFollowingCountAsync(Guid userGuid)
    {
        return await DbSet.CountAsync(f => f.FollowerGuid == userGuid);
    }

    /// <summary>统计指定用户的粉丝数量。</summary>
    public async Task<int> GetFollowerCountAsync(Guid userGuid)
    {
        return await DbSet.CountAsync(f => f.FolloweeGuid == userGuid);
    }

    /// <summary>新增关注关系并返回已跟踪的实体。</summary>
    public async Task<UserFollow> AddAsync(UserFollow follow)
    {
        var entry = await DbSet.AddAsync(follow);
        return entry.Entity;
    }

    /// <summary>删除指定的关注关系。</summary>
    public async Task DeleteAsync(Guid followerGuid, Guid followeeGuid)
    {
        var follow = await GetAsync(followerGuid, followeeGuid);
        if (follow is not null)
            DbSet.Remove(follow);
    }
}
