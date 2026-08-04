namespace Identity.Infrastructure.Repository;

public class UserRepository(IdentityDbContext userDbContext, IDistributedCache distributedCache)
    : IUserRepository
{
    /// <summary>手机验证码缓存 key 前缀（统一所有手机验证码方法的 key 格式）</summary>
    private const string PhoneCodeKeyPrefix = "PhoneCode";

    public IUnitOfWork UnitOfWork => userDbContext;

    public async ValueTask<User?> FindOneByUserAsync(Guid guid)
    {
        return await userDbContext.Users
            .Include(u => u.UserSafety)
            .Include(u => u.UserAccessFail)
            .AsSplitQuery()
            .Where(en => en.UserGuid == guid)
            .SingleOrDefaultAsync();
    }

    public async ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber));
        return await userDbContext.Users
            .Include(u => u.UserSafety)
            .Include(u => u.UserAccessFail)
            .AsSplitQuery()
            .Where(en => en.PhoneNumber!.AddressRegion == phoneNumber.AddressRegion &&
                         en.PhoneNumber.PhoneCode == phoneNumber.PhoneCode)
            .SingleOrDefaultAsync();
    }

    public async ValueTask<User?> FindOneByUserAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await userDbContext.Users
            .Include(u => u.UserSafety)
            .Include(u => u.UserAccessFail)
            .AsSplitQuery()
            .Where(en => en.UserEmail == email)
            .FirstOrDefaultAsync();
    }

    public async Task<ICollection<User>> FindAllByUserAsync()
    {
        return await userDbContext.Users
            .Include(u => u.UserSafety)
            .Include(u => u.UserAccessFail)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async ValueTask AddOneByUserAsync(User user)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));
        await userDbContext.Users.AddAsync(user);
    }

    public async ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message)
    {
        var find = await FindOneByUserAsync(phoneNumber);
        if (find is not null)
        {
            // F-07：落库登录历史
            var history = new UserLoginHistory(find.UserGuid, phoneNumber, message, find.UserEmail);
            await userDbContext.UserLoginHistories.AddAsync(history);
            await userDbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 原子递增登录失败计数（S-13）：ExecuteUpdate 在数据库端完成 +1，避免并发读改写竞态
    /// </summary>
    public async Task<int> IncrementAccessFaildCountAsync(Guid userGuid)
    {
        await userDbContext.Set<UserAccessFail>()
            .Where(x => x.UserGuid == userGuid)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AccessFaildCount, x => x.AccessFaildCount + 1));

        return await userDbContext.Set<UserAccessFail>()
            .Where(x => x.UserGuid == userGuid)
            .Select(x => x.AccessFaildCount)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 锁定用户登录失败记录（S-13）：原子更新 LockOutEnd
    /// </summary>
    public async Task LockUserAsync(Guid userGuid, DateTimeOffset lockOutEnd)
    {
        await userDbContext.Set<UserAccessFail>()
            .Where(x => x.UserGuid == userGuid)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockOutEnd, lockOutEnd));
    }

    public async ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code)
    {
        var key = $"{PhoneCodeKeyPrefix}{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        await distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
    }

    public async ValueTask SaveByEmailNumberAsync(string email, string code)
    {
        // 参数即邮箱，无需查库（修复此前为拿 UserEmail 而发起的冗余数据库查询）
        var key = $"emailAddress:{email}_{code}";
        await distributedCache.SetStringAsync(key, code,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
    }

    public Task UpdateByUserAsync(User user)
    {
        userDbContext.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteByUserAsync(User user)
    {
        userDbContext.Remove(user);
        return Task.CompletedTask;
    }

    public async ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber)
    {
        var key = $"{PhoneCodeKeyPrefix}{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        var code = await distributedCache.GetStringAsync(key);
        await distributedCache.RemoveAsync(key);
        return code ?? string.Empty;
    }

    public async ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber)
    {
        // 修复：此前 key 格式与 Save/Retirieve 不一致（大小写+分隔符都不同），导致永远取不到
        var key = $"{PhoneCodeKeyPrefix}{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        var code = await distributedCache.GetStringAsync(key);
        await distributedCache.RemoveAsync(key);
        return code ?? string.Empty;
    }
}