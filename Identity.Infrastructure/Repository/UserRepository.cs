using RabbitMQ.Client;

namespace Identity.Infrastructure.Repository;

public class UserRepository : IUserRepository
{

    private readonly ILogger<UserRepository> _logger;

    private readonly IdentityDbContext _userDbContext;
    public IUnitOfWork UnitOfWork => _userDbContext;

    public UserRepository(IdentityDbContext userDbContext, ILogger<UserRepository> logger)
    {
        _userDbContext = userDbContext ?? throw new ArgumentNullException(nameof(userDbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async ValueTask<User?> FindOneByUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow}]获取{userId}的数据");
            return await _userDbContext.Users
                .Include(en => en.UserSafety).Include(en => en.UserClaims)
                .FirstOrDefaultAsync(en => en.Id == userId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ X_X {DateTimeOffset.UtcNow}] 无法找到先关的{userId}信息");
            throw;
        }
    }

    public async ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        try
        {
            if (phoneNumber is not null)
                return await _userDbContext.Users
                    .Include(en => en.UserSafety).Include(en => en.UserClaims)
                    .FirstOrDefaultAsync(en => en.PhoneNumber.AddressRegion == phoneNumber.AddressRegion
                    && phoneNumber.PhoneCode == en.PhoneNumber.PhoneCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ X_X {DateTimeOffset.UtcNow}] 无法找到先关的{phoneNumber}信息");
            throw;
        }
        return null;
    }

    public async ValueTask AddOneByUserAsync(User user)
    {
        if (user is null)
            throw new ArgumentException(nameof(user));
        var _ = await _userDbContext.Users.AddAsync(user);
        _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]User Add! {user.Id}");
    }

    public async ValueTask<User?> FindOneByUserAsync(string email)
    {
        try
        {
            return await _userDbContext.Users
                .Include(en => en.UserSafety).Include(en => en.UserClaims)
                .FirstOrDefaultAsync(en => en.UserEmail == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ 〒▽〒 {DateTimeOffset.UtcNow}] 无法找到先关的{email}信息");
            throw;
        }
    }

    /// <summary>
    ///  更新用户信息
    /// </summary>
    /// <param name="user">传入用户更新类</param>
    /// <returns></returns>
    public async ValueTask UpdateByUserAsync(User user)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            var userTrc = await FindOneByUserAsync(user.Id);

            ArgumentNullException.ThrowIfNull(userTrc, nameof(userTrc));

            var updateCount = await _userDbContext.Users.Where(en => en.Id == user.Id).
                 ExecuteUpdateAsync(sets => sets
                 .SetProperty(en => en.UserName, user.UserName)
                 .SetProperty(en => en.UserEmail, user.UserEmail)
                 .SetProperty(en => en.Address, user.Address)
                 .SetProperty(en => en.UserRoleGuid, user.UserRoleGuid)                 
                 .SetProperty(en => en.ImageCover, user.ImageCover));
            if (updateCount == 0)
            {
                _logger.LogWarning($"[ 〒▽〒 {DateTime.UtcNow} ]User Not Update! {user.Id}");
                return;
            }
            else
            {
                _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow} 数据更新成功，一共更新了{updateCount}条目]");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{DateTime.UtcNow}]User Update Error! {user.Id}");
            throw;
        }
        finally
        {
            _logger.LogInformation($"[{DateTime.UtcNow}]User Update Complete! {user.Id}");
        }
    }

    /// <summary>
    ///  更新用户安全信息
    /// </summary>
    /// <param name="userSafety">传入用户安全信息</param>
    /// <returns></returns>
    public async ValueTask UpdateByUserSafetyAsync(UserSafety userSafety)
    {
        try
        {
            var updateCount = await _userDbContext.Users
                     .Where(en => en.UserSafety.Id == userSafety.Id)
            .ExecuteUpdateAsync(en => en

                 .SetProperty(en => en.UserSafety.BlackOrWhite, userSafety.BlackOrWhite)
                 .SetProperty(en => en.UserSafety.LockOutEnd, userSafety.LockOutEnd)
                 .SetProperty(en => en.UserSafety.SecurityStamp, userSafety.SecurityStamp)
                 .SetProperty(en => en.UserSafety.PasswordSalt, userSafety.PasswordSalt));
            ;
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow} 数据更新成功，一共更新了{updateCount}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ 〒▽〒 {DateTimeOffset.UtcNow}] 无法完成对{userSafety}");
            throw;
        }
    }

    /// <summary>
    ///  查询用户Claim信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <returns></returns>
    public async ValueTask<IEnumerable<UserClaim>> FindUserClaimsByUserAsync(Guid userGuid)
    {
        try
        {
            var data = await FindOneByUserAsync(userGuid);
            var claimData = data?.UserClaims
                .Where(c => c != null)
                .Cast<UserClaim>()
                .ToList() ?? new List<UserClaim>();
            if (claimData.Count == 0)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}]User Claims Not Found! {userGuid}");
                return Array.Empty<UserClaim>();
            }
            return claimData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ 〒▽〒 {DateTimeOffset.UtcNow}] 无法找到先关的{userGuid}信息");
            throw;
        }
    }

    public async ValueTask AddOneByUserClaimAsync(Guid userGuid, UserClaim userClaim)
    {
        var userData = await _userDbContext.Users.Include(en => en.UserClaims)
            .FirstOrDefaultAsync(en => en.Id == userGuid);

        if (userData is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}]User Not Found! {userGuid}");
            return;
        }
        userData.AddUserClaim(userClaim.ClaimType,userClaim.ClaimValue);
    }

    public ValueTask AddOneByUserClaimAsync(UserClaim userClaim)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///  更新用户Claim信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <param name="updateAction"></param>
    /// <returns></returns>
    public async ValueTask UpdateByUserClaimAsync(Guid userGuid,Action<User> updateAction)
    {
       var user=await _userDbContext.Users.Include(en=>en.UserClaims)
            .FirstOrDefaultAsync(en => en.Id == userGuid);
        if (user is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}]User Not Found! {userGuid}");
            return;
        }
        updateAction(user);        
    }

    /// <summary>
    ///  更新用户安全信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <param name="userSafetyAction"></param>
    /// <returns></returns>
    public async ValueTask UpdateByUserSafetyAsync(Guid userGuid, Action<UserSafety> userSafetyAction)
    {
        var userSafety = await _userDbContext.Users.Include(en=>en.UserSafety)
            .Where(en => en.Id == userGuid)
            .Select(en => en.UserSafety)
            .FirstOrDefaultAsync();

        if (userSafety is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}]User Safety Not Found! {userGuid}");
            return;
        }
        userSafetyAction(userSafety);
    }

    /// <summary>
    ///  更新用户信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <param name="userAction"></param>
    /// <returns></returns>
    public async ValueTask UpdateByUserAsync(Guid userGuid, Action<User> userAction)
    {
        var user = await _userDbContext.Users
            .FirstOrDefaultAsync(en => en.Id == userGuid);
        if (user is null)
        {
            _logger.LogWarning($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}]User Not Found! {userGuid}");
            return;
        }
        userAction(user);
    }

}