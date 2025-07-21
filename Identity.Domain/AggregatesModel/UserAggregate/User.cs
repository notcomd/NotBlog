using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class User : Entity, IAggregateRoot
{

    protected User()
    {
        UserGuid = Guid.CreateVersion7();
    }

    public Guid UserGuid { get; init; }

    public Guid UserRoleGuid { get; init; }

    public string? UserName { get; private set; }

    public Uri? ImageCover { get; private set; }

    public string UserEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    public Address? UserAddress { get; private set; }

    public UserAccessFail UserAccessFail { get; private set; }

    public UserSafety UserSafety { get; private set; }

    public DateTimeOffset CreateDatetime { get; init; }



    public static async Task<User> CreateByEmailUser(Guid userRoleGuid, string userEmail, string passwordHash)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (string.IsNullOrEmpty(userEmail))
            throw new ArgumentNullException(nameof(userEmail), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");
        

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ?? throw new ArgumentNullException("security stamp is null");
        var UserResult = new User
        {
            UserGuid = Guid.CreateVersion7(),
            UserRoleGuid = userRoleGuid,
            UserName = userEmail,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            ImageCover = null,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(Guid.CreateVersion7()) ?? throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(Guid.CreateVersion7(), stamp, salt.ToString(), BlackOrWhite.AuthorityWhite, UserStatus.Normal) ?? throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow

        };
        UserResult.AddDomainEvent(new UserStartedByEmailDomainEvent(userRoleGuid, userEmail, passwordHash, imageCover));
        return UserResult;
    }


    public static async Task<User> CreateByPhoneUser(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");
        //if (imageCover == null)
        //    throw new ArgumentNullException(nameof(imageCover), "Image cover cannot be null");

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ?? throw new ArgumentNullException("security stamp is null");
        var UserResult = new User
        {
            UserGuid = Guid.CreateVersion7(),
            UserRoleGuid = userRoleGuid,
            PhoneNumber = phoneNumber,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            ImageCover = null,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(Guid.CreateVersion7()) ?? throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(Guid.CreateVersion7(), stamp, salt.ToString(), BlackOrWhite.AuthorityWhite, UserStatus.Normal) ?? throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow

        };
        UserResult.AddDomainEvent(new UserStartedByPhoneDomainEvent(userRoleGuid, phoneNumber, passwordHash, imageCover));
        return UserResult;
    }





    /// <summary>
    /// 重新设置用户名
    /// </summary>
    /// <param name="userName"></param>
    public void RestartByUserName(string userName)
    {
        UserName = userName;
    }



    /// <summary>
    /// 重新设置用户头像
    /// </summary>
    /// <param name="imageCover"></param>
    public void RestartByImageCover(Uri imageCover)
    {
        ImageCover = imageCover;
    }



    public void ChangeByAddressAsync(ref Address userAddress)
    {
        UserAddress = userAddress;
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
    public async ValueTask ChangeByPasswordAsync(string password)
    {
        if (UserSafety.UserStatus == UserStatus.Locked)
        {
            throw new InvalidOperationException("用户已被锁定，无法修改密码");
        }
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException("your are set password is short!");
        }
        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException($"salt is null!");
        UserSafety.ResetByPasswordSalt(salt.ToString());
        PasswordHash = await HashH256Tool.CreateHash256Async(password, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    /// <param name="region">手机区号</param>
    /// <param name="phoneNumber">电话号码</param>
    /// <returns>
    /// 手机对象
    /// </returns>
    public void BandingByPhoneAsync(long region, string phoneNumber)
    {

        PhoneNumber = PhoneNumber.CreatePhoneNumber(region, phoneNumber) ?? throw new ArgumentNullException(nameof(PhoneNumber), "phone number is null");

        AddDomainEvent(new PhoneNumberBandingEvent(UserGuid, PhoneNumber.PhoneCode));

    }

    public bool VerifyByPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
        return PhoneNumber?.PhoneCode == phoneNumber.PhoneCode && PhoneNumber.AddressRegion == phoneNumber.AddressRegion;
    }

    public bool VerifyByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentNullException(nameof(email), "email is null or empty");
        return UserEmail == email;
    }


    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    public async Task<bool> VerifyByPassword(string passwordHash)
    {
        if (!await CheckByPasswordAsync(PasswordHash, passwordHash))
        {
            UserAccessFail.VerifyByAccessFaild(true);
            AddDomainEvent(new AccountLockedEvent(UserGuid));
        }
        return PasswordHash == passwordHash;
    }


    /// <summary>
    /// 设置信邮箱
    /// </summary>
    /// <param name="newEmail"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeByEmailAsync(
        [EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]
        ref string newEmail)
    {
        if (UserEmail == newEmail)
            throw new ArgumentException("需要不同的邮箱");
        UserEmail = newEmail;

    }

    /// <summary>
    /// 设置新密码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public void ChangeByPhoneAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber.PhoneCode == PhoneNumber!.PhoneCode)
            throw new ArgumentException("需要不要一样的号码");
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// 重新设置邮箱
    /// </summary>
    /// <param name="emailAddress"> 邮箱地址 </param>
    /// <returns></returns>
    public void RestartByEmailAsync([EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]string emailAddress)
    {
        if (UserEmail == emailAddress)
            throw new ArgumentException("需要不同的邮箱");
        UserEmail = emailAddress;
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
        return await HashH256Tool.VerifyPasswordValueTask(password, hashPassword, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));
    }
}