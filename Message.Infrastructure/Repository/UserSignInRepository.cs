namespace Message.Infrastructure.Repository;

/// <summary>用户签到记录仓储实现（UserSignIns 表）。</summary>
public class UserSignInRepository(MessageDbContext context) : IUserSignInRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserSignIn> DbSet = context.UserSignIns;

    /// <summary>获取指定用户在指定日期的签到记录，不存在时返回 null。</summary>
    public async Task<UserSignIn?> GetAsync(Guid userId, DateOnly date)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.UserId == userId && s.SignInDate == date);
    }

    /// <summary>判断指定用户在指定日期是否已签到。</summary>
    public async Task<bool> IsSignedInAsync(Guid userId, DateOnly date)
    {
        return await DbSet.AnyAsync(s => s.UserId == userId && s.SignInDate == date);
    }

    /// <summary>新增签到记录并返回已跟踪的实体。</summary>
    public async Task<UserSignIn> AddAsync(UserSignIn signIn)
    {
        var entry = await DbSet.AddAsync(signIn);
        return entry.Entity;
    }
}
