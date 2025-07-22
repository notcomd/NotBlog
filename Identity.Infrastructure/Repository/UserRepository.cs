namespace Identity.Infrastructure.Repository;

public class UserRepository : IUserRepository
{

    private readonly ILogger<UserRepository> _logger;
    private readonly IdentityDbContext _userDbContext;

    public UserRepository(IdentityDbContext userDbContext, ILogger<UserRepository> logger)
    {
        
        _userDbContext = userDbContext;
        _logger = logger;
    }

    public IUnitOfWork UnitOfWork => _userDbContext;

    public async ValueTask<User?> FindOneByUserAsync(Guid guid)
    {
        var data = await _userDbContext.Users.Where(en => en.UserGuid == guid)
            .SingleOrDefaultAsync();
        return data;
    }

    public async ValueTask<User?> FindOneByPhoneUserAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException("数据为空");
        var data = await _userDbContext.Users
            .Where(en => en.PhoneNumber!.AddressRegion == phoneNumber.AddressRegion &&
                         en.PhoneNumber.PhoneCode == phoneNumber.PhoneCode)
            .SingleOrDefaultAsync();
        return data;
    }

    public async ValueTask AddOneByUserAsync(User user)
    {
        await _userDbContext.Users.AddAsync(user);
        _logger.LogInformation($"[{DateTime.UtcNow}]User Add! {user.UserGuid}");
    }

    public async ValueTask<User?> FindOneByEmailUserAsync(string email)
    {
        return string.IsNullOrEmpty(email)
        ? null
        : await _userDbContext.Users.Where(en => en.UserEmail == email).SingleOrDefaultAsync();
    }

    //public ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message)
    //{
    //    var find = FindOneByUserAsync(phoneNumber).GetAwaiter().GetResult();
    //    if (find is not null)
    //    {
    //        var userid = find.UserGuid;
    //        //_userDbContext.FindAsync<User>(new UserLoginHistory(userid, phoneNumber, message, find.UserEmail));
    //    }

    //    return ValueTask.CompletedTask;
    //}

    public async ValueTask UpdateByUserAsync(User user)
    {

        if (user is null)
        {
            _logger.LogWarning($"[{DateTime.UtcNow}]User Not Found! {user.UserGuid}");
            return;
        }

        var userTrc = await FindOneByUserAsync(user.UserGuid);
        if (userTrc is null)
        {
            _logger.LogWarning($"[{DateTime.UtcNow}]User Not Found! {user.UserGuid}");
            return;
        }

        var needsUpdate = false;
        if (userTrc.UserName != user.UserName) needsUpdate = true;
        if (userTrc.UserEmail != user.UserEmail) needsUpdate = true;
        if (userTrc.PhoneNumber != user.PhoneNumber) needsUpdate = true;
        if (userTrc.UserSafety != user.UserSafety) needsUpdate = true;
        if (userTrc.UserAddress != user.UserAddress) needsUpdate = true;
        if (userTrc.UserRoleGuid != user.UserRoleGuid) needsUpdate = true;
        if (userTrc.PasswordHash != user.PasswordHash) needsUpdate = true;
        if(userTrc.ImageCover != user.ImageCover) needsUpdate = true;
        if (!needsUpdate)
        {
            _logger.LogInformation($"[{DateTime.UtcNow}]User Not Update! {user.UserGuid}");
            return;
        }

        try
        {

            var updateCount = await _userDbContext.Users.Where(en => en.UserGuid == user.UserGuid).
                  ExecuteUpdateAsync(sets => sets
                  .SetProperty(en => en.UserName, user.UserName)
                  .SetProperty(en => en.UserEmail, user.UserEmail)
                  .SetProperty(en => en.PhoneNumber, user.PhoneNumber)
                  .SetProperty(en => en.UserSafety, user.UserSafety)
                  .SetProperty(en => en.UserAddress, user.UserAddress)
                  .SetProperty(en => en.UserRoleGuid, user.UserRoleGuid)
                  .SetProperty(en => en.PasswordHash, user.PasswordHash)
                  .SetProperty(en=>en.ImageCover,user.ImageCover));

            if (updateCount == 0)
            {
                _logger.LogWarning($"[{DateTime.UtcNow}]User Not Update! {user.UserGuid}");
                return;
            }
            else
            {
                _logger.LogInformation($"[{DateTime.UtcNow}]User Update! {user.UserGuid}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{DateTime.UtcNow}]User Update Error! {user.UserGuid}");
            throw;
        }
        finally
        {
            _logger.LogInformation($"[{DateTime.UtcNow}]User Update Complete! {user.UserGuid}");
        }
    }

}