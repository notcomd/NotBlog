namespace Identity.Infrastructure.Repository;

public class UserRepository(IdentityDbContext userDbContext)
    : IUserRepository
{
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
            // P6：仅登记变更，提交由 UnitOfWork/外层事务统一负责（此前在此直接 SaveChanges，
            // 破坏事务原子性——若外层事务回滚，登录历史已落库无法撤销）
            var history = new UserLoginHistory(find.UserGuid, phoneNumber, message, find.UserEmail);
            await userDbContext.UserLoginHistories.AddAsync(history);
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

    /// <summary>
    /// 管理员分页查询用户：keyword 模糊匹配邮箱/用户名/手机号，按创建时间倒序。
    /// 仅投影安全字段（AdminUserBrief），绝不加载 PasswordHash/盐等敏感数据。
    /// </summary>
    public async Task<(ICollection<AdminUserBrief> Items, int Total)> GetPagedUsersAsync(string? keyword, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = userDbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(u =>
                u.UserEmail.Contains(kw) ||
                (u.UserName != null && u.UserName.Contains(kw)) ||
                (u.PhoneNumber != null && u.PhoneNumber.PhoneCode.Contains(kw)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreateDatetime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserBrief(
                u.UserGuid,
                u.UserName,
                u.UserEmail,
                u.AvatarUrl != null ? u.AvatarUrl.ToString() : null,
                u.PhoneNumber != null ? u.PhoneNumber.PhoneCode : null,
                u.CreateDatetime,
                (u.UserAccessFail != null && u.UserAccessFail.LockOutEnd != null &&
                 u.UserAccessFail.LockOutEnd.Value > DateTimeOffset.UtcNow) ||
                (u.UserSafety != null && u.UserSafety.LockOutEnd != null &&
                 u.UserSafety.LockOutEnd.Value > DateTimeOffset.UtcNow)))
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        return await userDbContext.Users.AnyAsync(u => u.UserEmail == email.Trim());
    }

}