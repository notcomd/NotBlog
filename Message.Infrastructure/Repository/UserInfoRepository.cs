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
}
