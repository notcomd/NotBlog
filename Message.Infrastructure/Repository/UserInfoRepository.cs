namespace Message.Infrastructure.Repository;

/// <summary>用户资料仓储实现（UserInfos 表）。</summary>
public class UserInfoRepository(MessageDbContext context) : IUserInfoRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserInfo> DbSet = context.UserInfos;

    /// <summary>按用户 ID 获取用户资料，不存在时返回 null。</summary>
    public async Task<UserInfo?> GetByUserIdAsync(Guid userId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>按邮箱获取用户资料（不区分大小写），不存在时返回 null。</summary>
    public async Task<UserInfo?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await DbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    /// <summary>按昵称精确匹配获取用户资料，最多返回指定数量。</summary>
    public async Task<IEnumerable<UserInfo>> GetByNickNameAsync(string nickName, int limit)
    {
        var normalized = nickName.Trim().ToLower();
        return await DbSet
            .Where(u => u.NickName != null && u.NickName.ToLower() == normalized)
            .OrderBy(u => u.UserId)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>按用户 ID 批量获取用户资料（用于在线名单等批量场景）。</summary>
    public async Task<IReadOnlyList<UserInfo>> GetByUserIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await DbSet
            .AsNoTracking()
            .Where(u => ids.Contains(u.UserId))
            .ToListAsync();
    }

    /// <summary>新增用户资料并返回已跟踪的实体。</summary>
    public async Task<UserInfo> AddAsync(UserInfo userInfo)
    {
        var entry = await DbSet.AddAsync(userInfo);
        return entry.Entity;
    }

    /// <summary>更新用户资料并返回已跟踪的实体。</summary>
    public async Task<UserInfo> UpdateAsync(UserInfo userInfo)
    {
        var entry = DbSet.Update(userInfo);
        return entry.Entity;
    }

    /// <summary>判断指定用户在指定日期是否已签到。</summary>
    public async Task<bool> IsSignedInAsync(Guid userId, DateOnly date)
    {
        return await context.UserSignIns.AnyAsync(s => s.UserId == userId && s.SignInDate == date);
    }
}
