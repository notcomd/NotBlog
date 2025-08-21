using Identity.Domain.Events;

using Microsoft.IdentityModel.Tokens;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class User : Entity, IAggregateRoot
{

    public Guid UserRoleGuid { get; private set; }

    public string UserName { get; private set; }

    public Uri? ImageCover { get; private set; }

    public string UserEmail { get; private set; }

    private string PasswordHash;

    public string? Address { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    private readonly List<UserClaim> _UserClaims = new List<UserClaim>();

    public IReadOnlyCollection<UserClaim?> UserClaims => _UserClaims.AsReadOnly();

    public UserAccessFail UserAccessFail { get; private set; }

    public UserSafety UserSafety { get; private set; }

    public DateTimeOffset CreateDatetime { get; init; }

    /// <summary>
    ///  无参构造函数
    /// </summary>
    protected User()
    {
        Id = Guid.CreateVersion7();
    }


    /// <summary>
    ///  邮件用户创建
    /// </summary>
    /// <param name="userRoleGuid"></param>
    /// <param name="userEmail"></param>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public User(Guid userRoleGuid, string userEmail, string passwordHash,
        DateTimeOffset dateTimeOffset)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (string.IsNullOrEmpty(userEmail))
            throw new ArgumentNullException(nameof(userEmail), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var stamp = HashHelper.GenerateSecurityStamp().Result;
        var salt = HashHelper.GenerateSaltValueTask().Result;
        passwordHash = HashHelper.CreateHash256Async(passwordHash, salt
            ?? throw new ArgumentNullException("salt is null!")).Result;

        var UserResult = new User
        {
            UserRoleGuid = userRoleGuid,
            UserName = userEmail,
            PasswordHash = passwordHash,
            CreateDatetime = dateTimeOffset,
            UserAccessFail = UserAccessFail.CreateByUserAccessFail(Id),
            UserSafety = UserSafety.CreateByUserSafety(Id, Encoding.UTF8.GetString(salt), stamp),
            UserEmail = userEmail,
            ImageCover = new Uri(uriString: string.Empty),
            PhoneNumber = null,
        };
        UserResult.AddDomainEvent(new CreatedByUserDomainEvent(UserResult.Id, userRoleGuid, userEmail, userEmail, null, dateTimeOffset));
    }


    /// <summary>
    ///  手机号码注册用户
    /// </summary>
    /// <param name="userRoleGuid"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public User(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash,
        DateTimeOffset dateTimeOffset)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var stamp = HashHelper.GenerateSecurityStamp().Result;
        var salt = HashHelper.GenerateSaltValueTask().Result;
        passwordHash = HashHelper.CreateHash256Async(passwordHash, salt
            ?? throw new ArgumentNullException("salt is null!")).Result;

        var UserResult = new User
        {
            UserRoleGuid = userRoleGuid,
            UserName = phoneNumber.PhoneCode,
            PasswordHash = passwordHash,
            CreateDatetime = dateTimeOffset,
            UserAccessFail = UserAccessFail.CreateByUserAccessFail(Id),
            UserSafety = UserSafety.CreateByUserSafety(Id, Encoding.UTF8.GetString(salt), stamp),
            UserEmail = string.Empty,
            ImageCover = new Uri(uriString: string.Empty),
            PhoneNumber = phoneNumber,
        };
        UserResult.AddDomainEvent(new CreatedByUserDomainEvent(UserResult.Id, userRoleGuid, phoneNumber.PhoneCode, string.Empty, phoneNumber, DateTimeOffset.UtcNow));
        //return UserResult;
    }

    /// <summary>
    /// 重新设置用户名
    /// </summary>
    /// <param name="userName"></param>
    public void ChangeByUserName(string userName)
    {
        if (!string.IsNullOrEmpty(userName))
        {
            if (UserName == userName)
            {
                throw new ArgumentException("需要不同的用户名");
            }
            if (userName.Length < 3 || userName.Length > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(userName), "用户名长度必须在3到50个字符之间");
            }
            UserName = userName;
        }
        else
        {
            throw new ArgumentNullException(nameof(userName), "user name is null or empty");
        }
    }

    /// <summary>
    /// 重新设置用户头像
    /// </summary>
    /// <param name="imageCover"></param>
    public void ChangeByImageCover(Uri imageCover)
    {
        if (imageCover is null)
        {
            throw new ArgumentNullException(nameof(imageCover), "image cover is null");
        }
        if (ImageCover is not null && ImageCover.Equals(imageCover))
        {
            throw new ArgumentException("需要不同的头像");
        }
        if (imageCover.IsAbsoluteUri)
        {
            ImageCover = imageCover;
        }
        else
        {
            throw new ArgumentException("image cover must be absolute uri", nameof(imageCover));
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="password">
    /// 密码
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 密码长度不能小于8位
    /// </exception>
    public async ValueTask ChangeByPasswordAsync(string password, byte[] salt, string stamp)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentNullException.ThrowIfNull(salt, nameof(salt));
        if (string.IsNullOrEmpty(stamp))
        {
            throw new ArgumentNullException(nameof(stamp), "security stamp is null or empty");
        }
        if (UserSafety.UserStatus == EnumUserStatus.Locked || UserSafety.BlackOrWhite == EnumBlackOrWhite.AuthorityBlack)
        {
            throw new InvalidOperationException("用户已被锁定，无法修改密码");
        }
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException(nameof(password));
        }
        var str = Encoding.UTF8.GetString(salt);
        PasswordHash = await HashHelper.CreateHash256Async(password, salt
            ?? throw new ArgumentNullException("salt is null!"));
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    /// <param name="region">手机区号</param>
    /// <param name="phoneNumber">电话号码</param>
    /// <returns>
    /// 手机对象
    /// </returns>
    public async Task BendingByPhoneAsync(long region, string phoneNumber)
    {
        PhoneNumber = PhoneNumber.CreatePhoneNumber(Id, region, phoneNumber) ?? throw new ArgumentNullException(nameof(PhoneNumber), "phone number is null");
        var stamp = await HashHelper.GenerateSecurityStamp();
        var code = await GenerateHelper.CreateRandomStringValueTask(9);
    }

    /// <summary>
    /// 验证手机号是否正确
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool IsVerifyByPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
        if (string.IsNullOrEmpty(phoneNumber.PhoneCode))
            throw new ArgumentNullException(nameof(phoneNumber.PhoneCode), "phone number code is null or empty");
        switch (phoneNumber.AddressRegion)
        {
            case (long)EnumAddressRegion.China:
            case (long)EnumAddressRegion.UnitedStates:
            case (long)EnumAddressRegion.SouthKorea:
            case (long)EnumAddressRegion.Japan:
            case (long)EnumAddressRegion.Taiwan:
            case (long)EnumAddressRegion.Hongkong:
            case (long)EnumAddressRegion.Singapore:
            case (long)EnumAddressRegion.XiaMen:
                break;
            default:
                throw new ArgumentException("phone number address region is error", nameof(phoneNumber.AddressRegion));
        }

        return PhoneNumber?.PhoneCode == phoneNumber.PhoneCode && PhoneNumber.AddressRegion == phoneNumber.AddressRegion;
    }

    /// <summary>
    /// 验证邮箱是否正确
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public bool IsVerifyByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentNullException(nameof(email), "email is null or empty");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("your set email is error ,pleas set again your email address!", nameof(email));
        return UserEmail == email;
    }

    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    public ValueTask<bool> IsVerifyByPasswordAsync(string passwordHash)=> HashHelper.VerifyPasswordValueTask(passwordHash, PasswordHash, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));

    public void ChangeByUserRole(Guid userRoleGuid)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (UserRoleGuid == userRoleGuid)
        {
            throw new ArgumentException("需要不同的用户角色", nameof(userRoleGuid));
        }
        UserRoleGuid = userRoleGuid;
    }

    /// <summary>
    /// 设置或跟新地址
    /// </summary>
    /// <param name="address"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeByAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 100)
            throw new ArgumentException(nameof(address));
        Address = address;
    }

    public void AddUserClaim(string claimType, string claimValue)
    {

        if (_UserClaims.Any(c => c.ClaimType == claimType))
        {
            throw new ArgumentException($"User already has a claim of type {claimType}", nameof(claimType));
        }
        var newClaim = UserClaim.CreateByUserClaim(Id, claimType, claimValue);
        _UserClaims.Add(newClaim);

    }

    public void UpdateClaim(string type, string newValue)
    {
        var claim = _UserClaims.FirstOrDefault(c => c.ClaimType == type)
            ?? throw new ArgumentException($"User does not have a claim of type {type}", nameof(type));
        claim.UpdateClaim(type, newValue);
    }

    public void RemoveClaim(string type)
    {
        var claim = _UserClaims.FirstOrDefault(c => c.ClaimType == type);
        if (claim is null)
        {
            throw new ArgumentException($"User does not have a claim of type {type}", nameof(type));
        }
        _UserClaims.Remove(claim);
    }

    public IEnumerable<Claim>? UserClaimToClaim(IEnumerable<UserClaim> userClaims)
    {
        if(userClaims is not null && userClaims.Any())
        {
            foreach (var userClaim in userClaims)
            {
                if (string.IsNullOrEmpty(userClaim.ClaimType) || string.IsNullOrEmpty(userClaim.ClaimValue))
                {
                    throw new InvalidOperationException("User claim type and value cannot be null or empty");
                }
                yield return userClaim.ToClaim();
            }
        }
        else
        {
            throw new ArgumentNullException(nameof(userClaims), "User claims cannot be null or empty");
        }
    }

}