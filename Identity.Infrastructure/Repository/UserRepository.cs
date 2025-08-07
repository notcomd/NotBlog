using Identity.Domain.AggregatesModel.UserAggregate;

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
            //_logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow}]插叙了{userId}的数据");
            return await _userDbContext.Users
                .Include(en => en.UserSafety).Include(en => en.UserClaimsReadOnly)
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
                    .Include(en => en.UserSafety).Include(en => en.UserClaimsReadOnly)
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
                .Include(en => en.UserSafety).Include(en => en.UserClaimsReadOnly)
                .FirstOrDefaultAsync(en => en.UserEmail == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ 〒▽〒 {DateTimeOffset.UtcNow}] 无法找到先关的{email}信息");
            throw;
        }
    }

    public async ValueTask UpdateByUserAsync(User user)
    {

        if (user is null)
        {
            _logger.LogWarning($"[ 〒▽〒 {DateTime.UtcNow}]User Not Found!");
            return;
        }

        var userTrc = await FindOneByUserAsync(user.Id);
        if (userTrc is null)
        {
            _logger.LogWarning($"[ (￣﹃￣) {DateTime.UtcNow}]User Not Found! {user.Id}");
            return;
        }

        var needsUpdate = false;
        if (userTrc.UserName != user.UserName) needsUpdate = true;
        if (userTrc.UserEmail != user.UserEmail) needsUpdate = true;
        if (userTrc.Address != user.Address) needsUpdate = true;
        if (userTrc.UserRoleGuid != user.UserRoleGuid) needsUpdate = true;
        if (userTrc.PasswordHash != user.PasswordHash) needsUpdate = true;
        if (userTrc.ImageCover != user.ImageCover) needsUpdate = true;
        if (!needsUpdate)
        {
            _logger.LogInformation($"[（*＾-＾*）{DateTime.UtcNow}]User Not Update! {user.Id}");
            return;
        }

        try
        {
            var updateCount = await _userDbContext.Users.Where(en => en.Id == user.Id).
                 ExecuteUpdateAsync(sets => sets
                 .SetProperty(en => en.UserName, user.UserName)
                 .SetProperty(en => en.UserEmail, user.UserEmail)
                 .SetProperty(en => en.Address, user.Address)
                 .SetProperty(en => en.UserRoleGuid, user.UserRoleGuid)
                 .SetProperty(en => en.PasswordHash, user.PasswordHash)
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

    public async ValueTask UpdateByUserSafety(UserSafety userSafety)
    {
        try
        {
            var updateCount = await _userDbContext.Users
                     .Where(en => en.UserSafety.Id == userSafety.Id)
            .ExecuteUpdateAsync(en => en
                 .SetProperty(en => en.UserSafety.IsDeleted, userSafety.IsDeleted)
                 .SetProperty(en => en.UserSafety.BlackOrWhite, userSafety.BlackOrWhite)
                 .SetProperty(en => en.UserSafety.LockOutEnd, userSafety.LockOutEnd)
                 .SetProperty(en => en.UserSafety.SecurityStamp, userSafety.SecurityStamp)
                 .SetProperty(en => en.UserSafety.PasswordSalt, userSafety.PasswordSalt)
                 .SetProperty(en => en.UserSafety.IsLockedOut, userSafety.IsLockedOut));
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow} 数据更新成功，一共更新了{updateCount}条目]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ 〒▽〒 {DateTimeOffset.UtcNow}] 无法完成对{userSafety}");
            throw;
        }
    }

}