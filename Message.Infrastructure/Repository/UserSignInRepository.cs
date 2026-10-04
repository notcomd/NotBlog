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

    /// <summary>查询指定用户在 [from, to] 闭区间内的签到日期（升序）。</summary>
    public async Task<IReadOnlyList<DateOnly>> GetDatesAsync(Guid userId, DateOnly from, DateOnly to)
    {
        return await DbSet
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.SignInDate >= from && s.SignInDate <= to)
            .Select(s => s.SignInDate)
            .OrderBy(d => d)
            .ToListAsync();
    }

    /// <summary>统计指定用户累计签到天数（全部历史）。</summary>
    public async Task<int> CountAsync(Guid userId)
    {
        return await DbSet.AsNoTracking().CountAsync(s => s.UserId == userId);
    }

    /// <summary>新增签到记录并返回已跟踪的实体。</summary>
    public async Task<UserSignIn> AddAsync(UserSignIn signIn)
    {
        var entry = await DbSet.AddAsync(signIn);
        return entry.Entity;
    }
}
