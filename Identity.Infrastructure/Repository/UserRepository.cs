namespace Identity.Infrastructure.Repository;

public class UserRepository : IUserRepository
{

    private readonly ILogger<UserRepository> _logger;

    private readonly IdentityDbContext _userDbContext;

    public IUnitOfWork UnitOfWork => _userDbContext;

    private readonly INotDateTime _notDateTime;

    public UserRepository(IdentityDbContext userDbContext, ILogger<UserRepository> logger, INotDateTime notDateTime)
    {
        _userDbContext = userDbContext ?? throw new ArgumentNullException(nameof(userDbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notDateTime = notDateTime;
    }

    public async ValueTask<User?> FindOneByUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("[（*＾-＾*）] 获取用户数据 UserId:{UserId}", userId);
            return await _userDbContext.Users
                .Include(en => en.UserSafety).Include(en => en.UserClaims).Include(en => en.PhoneNumber)
                .FirstOrDefaultAsync(en => en.Id == userId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[X_X] 无法找到相关的用户信息 UserId:{UserId}", userId);
            throw;
        }
    }

    public async ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        try
        {
            if (phoneNumber is not null)
                return await _userDbContext.Users
                    .Include(en => en.UserSafety).Include(en => en.UserClaims).Include(en => en.PhoneNumber)
                    .FirstOrDefaultAsync(en => en.PhoneNumber.AddressRegion == phoneNumber.AddressRegion
                    && phoneNumber.PhoneCode == en.PhoneNumber.PhoneCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[X_X] 无法找到相关的电话号码信息 PhoneNumber:{PhoneNumber}", phoneNumber);
            throw;
        }
        return null;
    }

    public async ValueTask AddOneByUserAsync(User user)
    {
        if (user is null)
            throw new ArgumentException(nameof(user));
        var _ = await _userDbContext.Users.AddAsync(user);
        _logger.LogInformation("[（*＾-＾*）] 用户添加成功 UserId:{UserId}", user.Id);
    }

    public async ValueTask<User?> FindOneByUserAsync(string email)
    {
        try
        {
            return await _userDbContext.Users
                .Include(en => en.UserSafety).Include(en => en.UserClaims)
                .Include(en => en.PhoneNumber).Include(en => en.UserAccessFail)
                .FirstOrDefaultAsync(en => en.UserEmail == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法找到相关的邮箱信息 Email:{Email}", email);
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

                 .SetProperty(en => en.Address, user.Address)

                 .SetProperty(en => en.ImageCover, user.ImageCover));
            if (updateCount == 0)
            {
                _logger.LogWarning("[〒▽〒] 用户未更新 UserId:{UserId}", user.Id);
                return;
            }
            else
            {
                _logger.LogInformation("[（*＾-＾*）] 数据更新成功，一共更新了{UpdateCount}条目 UserId:{UserId}", updateCount, user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[X_X] 用户更新错误 UserId:{UserId}", user.Id);
            throw;
        }
        finally
        {
            _logger.LogInformation("[（￣︶￣）] 用户更新完成 UserId:{UserId}", user.Id);
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
            _logger.LogInformation("[（*＾-＾*）] 安全信息更新成功，一共更新了{UpdateCount}条目 UserSafetyId:{UserSafetyId}", updateCount, userSafety.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法完成对用户安全信息的更新 UserSafetyId:{UserSafetyId}", userSafety.Id);
            throw;
        }
    }

    /// <summary>
    ///  查询用户Claim信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <returns></returns>
    public async ValueTask<IEnumerable<UserClaim>?> FindUserClaimsByUserAsync(Guid userGuid)
    {
        try
        {

            var data = await FindOneByUserAsync(userGuid);
            if (data is null)
                throw new ArgumentNullException(nameof(userGuid), "User is null");
            //return data.UserClaims.Select(en=>new ValueTask<IEnumerable<UserClaim>>((IEnumerable<UserClaim>)en)).FirstOrDefault().Result;
            if (data.UserClaims is not null && data.UserClaims.Count != 0)
            {
                return data.UserClaims.Where(claim => claim != null).Select(claim => claim!).ToList();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法找到相关的用户信息 UserId:{UserId}", userGuid);
            return null;
        }
    }

    public async ValueTask AddOneByUserClaimAsync(Guid userGuid, UserClaim userClaim)
    {
        var userData = await _userDbContext.Users.Include(en => en.UserClaims)
            .FirstOrDefaultAsync(en => en.Id == userGuid);

        if (userData is null)
        {
            _logger.LogWarning("[(≧ ﹏ ≦)] 未找到用户 UserId:{UserId}", userGuid);
            return;
        }
        userData.AddUserClaim(userClaim.ClaimType, userClaim.ClaimValue);
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
    public async ValueTask UpdateByUserClaimAsync(Guid userGuid, Func<User, Task> updateAction)
    {
        try
        {
            var user = await _userDbContext.Users.Include(en => en.UserClaims)
             .FirstOrDefaultAsync(en => en.Id == userGuid);
            if (user is null)
            {
                _logger.LogWarning("[(≧ ﹏ ≦)] 未找到用户 UserId:{UserId}", userGuid);
                return;
            }
            await updateAction(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法完成对用户声明的更新 UserId:{UserId}", userGuid);
            throw;
        }

    }


    /// <summary>
    ///  更新用户安全信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <param name="userSafetyAction"></param>
    /// <returns></returns>
    public async ValueTask UpdateByUserSafetyAsync(string findObject, Func<User, Task> userSafetyAction)
    {
        try
        {
            var userSafety = await _userDbContext.Users.Include(en => en.UserSafety)
                .FirstOrDefaultAsync(en => en.UserEmail == findObject);

            if (userSafety is null)
            {
                _logger.LogWarning("[(≧ ﹏ ≦)] 未找到用户安全信息 Email:{Email}", findObject);
                return;
            }
            await userSafetyAction(userSafety);
            //_userDbContext.Entry(userSafety).State = EntityState.Modified;
            //_userDbContext.Entry(userSafety.UserSafety).State = EntityState.Modified;
            _logger.LogInformation("[（*＾-＾*）] 用户安全信息更新成功 Email:{Email}", findObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法完成对用户安全信息的更新 Email:{Email}", findObject);
            throw;
        }

    }

    /// <summary>
    ///  更新用户信息
    /// </summary>
    /// <param name="userGuid"></param>
    /// <param name="userAction"></param>
    /// <returns></returns>
    public async ValueTask UpdateByUserAsync(string userEmail, Func<User, Task> userAction)
    {
        try
        {
            var user = await _userDbContext.Users
           .FirstOrDefaultAsync(en => en.UserEmail == userEmail);
            if (user is null)
            {
                _logger.LogWarning("[(≧ ﹏ ≦)] 未找到用户 Email:{Email}", userEmail);
                return;
            }
            await userAction(user);
            // _userDbContext.Entry(user).State = EntityState.Modified;
            _logger.LogInformation("[(＾-＾*)] 用户数据更新成功 Email:{Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 无法完成对用户的更新 Email:{Email}", userEmail);
            throw;
        }
        finally
        {
            _logger.LogInformation("[(￣︶￣)↗] 用户更新完成 Email:{Email}", userEmail);
        }

    }

    public async ValueTask<IEnumerable<User>> FindAllByUserAsync()
    {
        try
        {
            return await _userDbContext.Users.Include(en => en.UserClaims)
                    .Include(en => en.UserSafety).Include(en => en.UserAccessFail)
                    .Include(en => en.PhoneNumber).ToListAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[〒▽〒] 获取用户列表失败");
            throw;
        }
    }

}