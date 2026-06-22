namespace Identity.Infrastructure.Repository;

public class UserRepository(IdentityDbContext userDbContext, IDistributedCache distributedCache)
    : IUserRepository
{
    public IUnitOfWork UnitOfWork => userDbContext;

    public async ValueTask<User?> FindOneByUserAsync(Guid guid)
    {
        return await userDbContext.Users.Where(en => en.UserGuid == guid)
            .SingleOrDefaultAsync();
    }

    public async ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException("数据为空");
        return await userDbContext.Users
            .Where(en => en.PhoneNumber!.AddressRegion == phoneNumber.AddressRegion &&
                         en.PhoneNumber.PhoneCode == phoneNumber.PhoneCode)
            .SingleOrDefaultAsync();
    }

    public async ValueTask AddOneByUserAsync(User user)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));
        await userDbContext.Users.AddAsync(user);
    }

    public async ValueTask<User?> FindOneByUserAsync(string email)
    {
        return await userDbContext.Users
            .Where(en => en.UserEmail.Equals(email))
            .SingleOrDefaultAsync();
    }

    public async ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message)
    {
        var find = await FindOneByUserAsync(phoneNumber);
        if (find is not null)
        {
            var userid = find.UserGuid;
        }
    }


    public async ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code)
    {
        var key = $"PhoneCode{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        await distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
    }

    public async ValueTask SaveByEmailNumberAsync(string email, string code)
    {
        var data = await userDbContext.Users.SingleOrDefaultAsync(en => en.UserEmail == email);
        if (data is null) return;
        var key = $"emailAddress:{data.UserEmail}_{code}";
        await distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        // return ValueTask.CompletedTask;
    }

    public ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber)
    {
        throw new NotImplementedException();
    }


    public async ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber)
    {
        var key = $"phoneCode{phoneNumber.PhoneCode},phoneAddressRegion{phoneNumber.AddressRegion}";
        var code = await distributedCache.GetStringAsync(key);
        await distributedCache.RemoveAsync(key);
        return code ?? string.Empty;
    }
}