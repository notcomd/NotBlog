namespace Identity.Infrastructure.Repository;

public class Author2Repository(IdentityDbContext identityDbContext) : IAuthor2Repository
{
    public IUnitOfWork UnitOfWork => identityDbContext;


    public async Task AddAuthor2Async(Author2 author2)
    {
        await identityDbContext.AddAsync(author2);
    }

    public async Task<Author2?> FindAuthor2ByUserIdAsync(Guid userId)
    {
        var data = await identityDbContext
            .UserExternalLogins.FirstOrDefaultAsync(x => x.UserId == userId);
        return data;
    }

    public async Task<Author2?> FindAuthor2ByProviderAsync(string loginProvider, string providerKey)
    {
        var data = await identityDbContext
            .UserExternalLogins
            .FirstOrDefaultAsync(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey);
        return data;
    }

    public Task<Author2?> FindAuthor2ByUserIdAndProviderAsync(Guid userId, string loginProvider)
    {
        var data = identityDbContext
            .UserExternalLogins
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LoginProvider == loginProvider);
        return data;
    }

    public async Task<IEnumerable<Author2>> GetAllAuthor2sAsync()
    {
       var data= await identityDbContext.UserExternalLogins.ToListAsync();
       return data;
    }
}