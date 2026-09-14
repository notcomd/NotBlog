namespace Message.Infrastructure.Repository;

/// <summary>用户资料仓储实现（UserInfos 表）。</summary>
public class UserInfoRepository(MessageDbContext context) : IUserInfoRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserInfo> DbSet = context.UserInfos;

    public async Task<UserInfo?> GetByUserIdAsync(Guid userId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<UserInfo?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await DbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<IEnumerable<UserInfo>> GetByNickNameAsync(string nickName, int limit)
    {
        var normalized = nickName.Trim().ToLower();
        return await DbSet
            .Where(u => u.NickName != null && u.NickName.ToLower() == normalized)
            .OrderBy(u => u.UserId)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<UserInfo> AddAsync(UserInfo userInfo)
    {
        var entry = await DbSet.AddAsync(userInfo);
        return entry.Entity;
    }

    public async Task<UserInfo> UpdateAsync(UserInfo userInfo)
    {
        var entry = DbSet.Update(userInfo);
        return entry.Entity;
    }

    public async Task<bool> IsSignedInAsync(Guid userId, DateOnly date)
    {
        return await context.UserSignIns.AnyAsync(s => s.UserId == userId && s.SignInDate == date);
    }
}
