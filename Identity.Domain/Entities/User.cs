namespace Identity.Domain.Entities;

public class User : IAggregateRoot
{


    /// <summary>
    /// 邮箱创建
    /// </summary>
    /// <param name="userRoleGuid">角色的guid</param>
    /// <param name="userEmail">注册的邮箱</param>
    /// <param name="passwordHash">密码哈希值</param>
    /// <param name="salt">加盐</param>
    /// <param name="imageCover">头像</param>
    public User(Guid userRoleGuid, string userEmail, string passwordHash, string salt, Uri imageCover)
    {
        UserRoleGuid = userRoleGuid;
        UserName = userEmail;
        PasswordHash = passwordHash;
        Salt = salt;
        ImageCover = imageCover;
        UserAccessFail = new UserAccessFail(this);
    }

    /// <summary>
    /// 创建用户(手机创建）
    /// </summary>
    /// <param name="userRoleGuid"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="passwordHash"></param>
    /// <param name="salt"></param>
    /// <param name="imageCover"></param>
    public User(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash, string salt,
        Uri imageCover)
    {
        UserRoleGuid = userRoleGuid;
        UserName = phoneNumber.PhoneCode;
        UserPhone = phoneNumber;
        PasswordHash = passwordHash;
        Salt = salt;
        ImageCover = imageCover;
        UserAccessFail = new UserAccessFail(this);
    }

    /// <summary>
    /// 用户唯一GUID
    /// </summary>
    public Guid UserGuid { get; init; } = Guid.CreateVersion7();
    /// <summary>
    /// 角色
    /// </summary>
    public Guid UserRoleGuid { get; init; }
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; private set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? UserEmail { get; private set; }
    /// <summary>
    /// 哈希密码
    /// </summary>
    public string PasswordHash { get; private set; }
    /// <summary>
    /// 加言
    /// </summary>
    public string Salt { get; private set; }
    public PhoneNumber? UserPhone { get; private set; }
    /// <summary>
    /// 用户地址
    /// </summary>
    public string? UserAddress { get; private set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(TypeName = "timestamp with time zone")]
    public DateTimeOffset CreateDatetime { get; init; } = DateTimeOffset.Now;
    /// <summary>
    /// 登录失败次数
    /// </summary>
    public UserAccessFail UserAccessFail { get; init; }
    /// <summary>
    /// 黑名单或者白名单
    /// </summary>
    public BlackOrWhite? BlackOrWhite { get; private set; } = Entities.BlackOrWhite.AuthorityWhite;
    /// <summary>
    /// 权限
    /// </summary>
    public LimitsOfAuthority LimitsOfAuthority { get; private set; } = LimitsOfAuthority.AuthorityUser;
    /// <summary>
    /// 头像
    /// </summary>
    public Uri ImageCover { get; private set; }

    /// <summary>
    /// 重新设置用户名
    /// </summary>
    /// <param name="userName"></param>
    public void RestartByUserName(string userName)
    {
        UserName = userName;
    }

    /// <summary>
    /// 重新设置用户地址
    /// </summary>
    /// <param name="userAddress"></param>
    public void RestartByUserAddress(ref string userAddress)
    {
        UserAddress = userAddress;
    }

    /// <summary>
    /// 重新设置用户头像
    /// </summary>
    /// <param name="imageCover"></param>
    public void RestartByImageCover(Uri imageCover)
    {
        ImageCover = imageCover;
    }

    public ValueTask<User> ChangeByAddressAsync(ref string userAddress)
    {
        UserAddress = userAddress;
        return new ValueTask<User>(this);
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
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException("your are set password is short!");
        }
        PasswordHash = await HashH256Tool.CreateHash256Async(password, Encoding.UTF8.GetBytes(Salt));
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    /// <param name="region">手机区号</param>
    /// <param name="phoneNumber">电话号码</param>
    /// <returns>
    /// 手机对象
    /// </returns>
    public static Task<PhoneNumber> BandingByPhoneAsync(long region, string phoneNumber)
    {
        return Task.FromResult(new PhoneNumber
        {
            PhoneCode = phoneNumber,
            AddressRegion = region
        });
    }


    /// <summary>
    /// 设置信邮箱
    /// </summary>
    /// <param name="newEmail"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ValueTask ChangeByEmailAsync(
        [EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]
        ref string newEmail)
    {
        if (UserEmail == newEmail) throw new ArgumentException("需要不同的邮箱");
        UserEmail = newEmail;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 设置新密码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ValueTask ChangeByPhoneAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber.PhoneCode == UserPhone!.PhoneCode) throw new ArgumentException("需要不要一样的号码");
        UserPhone = phoneNumber;
        return ValueTask.CompletedTask;
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
    public ValueTask<bool> CheckByPasswordAsync(string hashPassword, string password, byte[] salt)
    {
        return HashH256Tool.VerifyPasswordValueTask(password, hashPassword, salt);
    }

    /// <summary>
    /// 添加黑名单或者白名单
    /// </summary>
    /// <param name="blackOrWhite"></param>
    /// <returns></returns>
    public void RestartByBlackOrWhite(BlackOrWhite blackOrWhite)
    {
        BlackOrWhite = blackOrWhite;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="password"></param>
    /// <param name="salt"></param>
    /// <returns></returns>
    public async ValueTask<User> ChangeByPasswordValueTask(string password, byte[] salt)
    {
        if (!await HashH256Tool.VerifyPasswordValueTask(password, PasswordHash, salt))
        {
            PasswordHash = await HashH256Tool.CreateHash256Async(password, salt);
        }
        return this;
    }
}