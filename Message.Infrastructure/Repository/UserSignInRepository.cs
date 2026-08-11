namespace Message.Infrastructure.Repository;

/// <summary>用户签到记录仓储实现（UserSignIns 表）。</summary>
public class UserSignInRepository(MessageDbContext context) : IUserSignInRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<UserSignIn> DbSet = context.UserSignIns;

    public async Task<UserSignIn?> GetAsync(Guid userId, DateOnly date)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.UserId == userId && s.SignInDate == date);
    }

    public async Task<bool> IsSignedInAsync(Guid userId, DateOnly date)
    {
        return await DbSet.AnyAsync(s => s.UserId == userId && s.SignInDate == date);
    }

    public async Task<UserSignIn> AddAsync(UserSignIn signIn)
    {
        var entry = await DbSet.AddAsync(signIn);
        return entry.Entity;
    }
}
