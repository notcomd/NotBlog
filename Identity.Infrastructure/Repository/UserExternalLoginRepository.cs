namespace Identity.Infrastructure.Repository;

public class UserExternalLoginRepository(IdentityDbContext dbContext) : IUserExternalLoginRepository
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async Task AddAsync(UserExternalLogin login)
    {
        await dbContext.UserExternalLogins.AddAsync(login);
    }

    public async Task<IReadOnlyList<UserExternalLogin>> FindByUserIdAsync(Guid userId)
    {
        return await dbContext.UserExternalLogins
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserExternalLogin?> FindByProviderAsync(LoginProviderType provider, string providerKey)
    {
        return await dbContext.UserExternalLogins
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderKey == providerKey);
    }

    public async Task<UserExternalLogin?> FindByUserIdAndProviderAsync(Guid userId, LoginProviderType provider)
    {
        return await dbContext.UserExternalLogins
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider);
    }

    public async Task<IReadOnlyList<UserExternalLogin>> GetAllAsync()
    {
        return await dbContext.UserExternalLogins.ToListAsync();
    }
}