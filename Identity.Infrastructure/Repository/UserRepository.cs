namespace Identity.Infrastructure.Repository;

public class UserRepository : IUserRepository
{
    private readonly IDistributedCache _distributedCache;

    private readonly IdentityDbContext _userDbContext;

    public UserRepository(IdentityDbContext userDbContext, IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        _userDbContext = userDbContext;
    }

    public IUnitOfWork UnitOfWork => _userDbContext;

    public ValueTask<User?> FindOneByUserAsync(Guid guid)
    {
        var data = _userDbContext.Users.Where(en => en.UserGuid == guid)
            .SingleOrDefaultAsync().GetAwaiter()
            .GetResult();
        return new ValueTask<User?>(data);
    }

    public ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException("数据为空");
        var data = _userDbContext.Users
            .Where(en => en.PhoneNumber.AddressRegion == phoneNumber.AddressRegion &&
                         en.PhoneNumber.PhoneCode == phoneNumber.PhoneCode)
            .SingleOrDefaultAsync().GetAwaiter().GetResult();
        return new ValueTask<User?>(data);
    }

    public ValueTask AddOneByUserAsync(User user)
    {
        _userDbContext.Users.AddAsync(user).GetAwaiter();
        return ValueTask.CompletedTask;
    }

    public ValueTask<User?> FindOneByUserAsync(string email)
    {
        var data = _userDbContext.Users.Where(en => en.UserEmail == email).SingleOrDefaultAsync().GetAwaiter()
            .GetResult();
        return new ValueTask<User?>(data);
    }

    public ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message)
    {
        var find = FindOneByUserAsync(phoneNumber).GetAwaiter().GetResult();
        if (find is not null)
        {
            var userid = find.UserGuid;
            //_userDbContext.FindAsync<User>(new UserLoginHistory(userid, phoneNumber, message, find.UserEmail));
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     保存验证马
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code)
    {
        var key = $"PhoneCode{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        _distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveByEmailNumberAsync(string email, string code)
    {
        var data = _userDbContext.Users.SingleOrDefaultAsync(en => en.UserEmail == email).GetAwaiter().GetResult();
        if (data is null) return ValueTask.CompletedTask;
        var key = $"emailAddress:{data.UserEmail}_{code}";
        _distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        //string key = $"emailAddress:{data.UserEmail}_{code}";
        return ValueTask.CompletedTask;
    }

    public ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 验证过后直接删除
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    public ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber)
    {
        var key = $"phoneCode{phoneNumber.PhoneCode},phoneAddressRegion{phoneNumber.AddressRegion}";
        var code = _distributedCache.GetStringAsync(key).GetAwaiter().GetResult()!;
        _distributedCache.RemoveAsync(key);
        return new ValueTask<string>(code);
    }
}