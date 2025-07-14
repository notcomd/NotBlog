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

    
}