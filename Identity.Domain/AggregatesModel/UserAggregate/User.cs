using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class User : Entity, IAggregateRoot
{

    public Guid UserRoleGuid { get; init; }

    public string? UserName { get; private set; }

    public Uri? ImageCover { get; private set; }

    public string UserEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public string? Address { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    private IList<UserClaim?> UserClaims { get; set; }

    public IEnumerable<UserClaim?> UserClaimsReadOnly => UserClaims.AsReadOnly();

    public UserAccessFail UserAccessFail { get; private set; }

    public UserSafety UserSafety { get; private set; }

    public DateTimeOffset CreateDatetime { get; init; }

    /// <summary>
    ///  无参构造函数
    /// </summary>
    protected User()
    {

    }

    /// <summary>
    ///  邮件用户创建
    /// </summary>
    /// <param name="userRoleGuid"></param>
    /// <param name="userEmail"></param>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueTask<User> CreateByEmailUser(Guid userRoleGuid, string userEmail, string passwordHash, DateTimeOffset dateTimeOffset)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (string.IsNullOrEmpty(userEmail))
            throw new ArgumentNullException(nameof(userEmail), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var UserResult = new User
        {
            Id = Guid.CreateVersion7(),
            UserRoleGuid = userRoleGuid,
            UserName = userEmail,
            PasswordHash = passwordHash,
                      
            CreateDatetime = dateTimeOffset
        };
        UserResult.AddDomainEvent(new CreateUserByEmailDomainEvent(UserResult, userEmail, userEmail, DateTimeOffset.UtcNow));
        return new ValueTask<User>(UserResult);
    }

    /// <summary>
    ///  手机号码注册用户
    /// </summary>
    /// <param name="userRoleGuid"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueTask<User> CreateByPhoneUser(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash, DateTimeOffset dateTimeOffset)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var UserResult = new User
        {
            Id = Guid.CreateVersion7(),
            UserRoleGuid = userRoleGuid,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            CreateDatetime = dateTimeOffset
        };
        UserResult.AddDomainEvent(new CreateUserByPhoneDomainEvent(UserResult, phoneNumber, phoneNumber.PhoneCode, DateTimeOffset.UtcNow));
        return new ValueTask<User>(UserResult);
    }

    /// <summary>
    /// 重新设置用户名
    /// </summary>
    /// <param name="userName"></param>
    public void SetOrRestartByUserName(string userName)
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
    public void SetOrRestartByImageCover(Uri imageCover)
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
        if (UserSafety.UserStatus == EnumUserStatus.Locked || UserSafety.BlackOrWhite == EnumBlackOrWhite.AuthorityBlack)
        {
            throw new InvalidOperationException("用户已被锁定，无法修改密码");
        }
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException(nameof(password));
        }
        var str = salt.ToString() ?? throw new ArgumentNullException(nameof(salt));
        UserSafety.SetOrResetByPasswordSalt(str);
        UserSafety.SetOrResetBySecurityStamp(stamp);
        PasswordHash = await HashH256Tool.CreateHash256Async(password, salt
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
    public  void SetOrRestByPhoneAsync(long region, string phoneNumber)
    {
        PhoneNumber = PhoneNumber.CreatePhoneNumber(Id, region, phoneNumber) ?? throw new ArgumentNullException(nameof(PhoneNumber), "phone number is null");
        AddDomainEvent(new PhoneNumberBandingEvent(Id, PhoneNumber.PhoneCode));
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
        if (phoneNumber.AddressRegion <= 0)
            throw new ArgumentOutOfRangeException(nameof(phoneNumber.AddressRegion), "phone number address region is less than or equal to zero");

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
    public async Task<bool> IsVerifyByPassword(string passwordHash)
    {
        if (!await CheckByPasswordAsync(PasswordHash, passwordHash))
        {
            UserAccessFail.VerifyByAccessFaild();
            AddDomainEvent(new AccountLockedEvent(Id));
        }
        return PasswordHash == passwordHash;
    }

    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    /// <param name="hashPassword">hash密码</param>
    /// <param name="password">密码</param>
    /// <param name="salt">加盐</param>
    /// <returns>
    ///返回一个布尔值，false：密码错误，true：密码正确
    /// </returns>
    private async Task<bool> CheckByPasswordAsync(string hashPassword, string password)
    {
        ArgumentNullException.ThrowIfNull(password, nameof(password));
        return await HashH256Tool.VerifyPasswordValueTask(password, hashPassword, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));
    }

    /// <summary>
    ///  
    /// </summary>
    /// <param name="salt"></param>
    /// <param name="stamp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public void  SetOrRestByUserSafety(string salt, string stamp)
    {
        if (string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(stamp))
            throw new ArgumentNullException(nameof(salt));
        UserSafety.CreateByUserSafety(Id, salt, stamp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public void SetOrRestByUserClaim(Claim claim)
    {
        if (claim == null)
            throw new ArgumentNullException(nameof(claim));
        UserClaim.CreateByUserClaimAsync(Id, claim);
        // return 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task SetOrRestByUserAccessFail()
    {
        await UserAccessFail.CreateByUserAccessFailAsync(Id);
    }

    /// <summary>
    ///  改变用户的账号状态
    /// </summary>
    /// <param name="enumBlackOrWhite"></param>
    /// <param name="enumUserStatus"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeByUserStatusAsync(EnumBlackOrWhite enumBlackOrWhite, EnumUserStatus enumUserStatus)
    {
        if (enumBlackOrWhite == EnumBlackOrWhite.AuthorityBlack && enumUserStatus != EnumUserStatus.Locked)
        {
            throw new ArgumentException("黑名单用户状态必须为锁定状态", nameof(enumUserStatus));
        }
        UserSafety.SetOrResetByBlackOrWhiteAndUserStatus(enumBlackOrWhite, enumUserStatus);

    }

    /// <summary>
    /// 设置或跟新地址
    /// </summary>
    /// <param name="address"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetOrRestByAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 100)
            throw new ArgumentException(nameof(address));

        Address = address;
    }


}