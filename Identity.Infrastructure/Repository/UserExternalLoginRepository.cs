namespace Identity.Infrastructure.Repository;

public class UserExternalLoginRepository(IdentityDbContext dbContext) : IUserExternalLoginRepository
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async Task AddAsync(UserExternalLogin login)
    {
        await dbContext.UserExternalLogins.AddAsync(login);
    }

    public Task DeleteAsync(UserExternalLogin login)
    {
        dbContext.UserExternalLogins.Remove(login);
        return Task.CompletedTask;
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

    /// <summary>
    /// 按 provider + providerKey 查找（与 <see cref="FindByProviderAsync"/> 等价，保留以兼容既有调用方）。
    /// 新代码请直接使用 <see cref="FindByProviderAsync"/>。
    /// </summary>
    public Task<UserExternalLogin?> FindOneByUserIdAndProviderAsync(LoginProviderType provider, string providerKey)
        => FindByProviderAsync(provider, providerKey);
}