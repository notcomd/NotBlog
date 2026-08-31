using Identity.Domain.Events;
using Notcomd.Token.JWT.Security;

namespace Identity.Domain.Entities.UserAggregate;

public class User : Entity<Guid>, IAggregateRoot
{
    protected User()
    {
        UserGuid = Guid.CreateVersion7();
        UserRoleGuid ??= new List<Guid>();
        AuthorGuids ??= new List<Guid>();
        CreateDatetime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    /// 用户角色ID
    /// </summary>
    public List<Guid> UserRoleGuid { get; private set; }

    /// <summary>
    /// 外部登入关联
    /// </summary>
    public List<Guid> AuthorGuids { get; private set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// 用户头像
    /// </summary>
    public Uri? AvatarUrl { get; private set; }

    /// <summary>
    /// 用户邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "Error Email Address!")]
    public string UserEmail { get; private set; } = null!;

    /// <summary>
    /// 用户密码哈希
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// 用户手机号
    /// </summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>
    /// 用户地址
    /// </summary>
    public Address? UserAddress { get; private set; }

    /// <summary>
    /// 用户访问失败
    /// </summary>
    public UserAccessFail UserAccessFail { get; private set; } = null!;

    /// <summary>
    /// 用户安全
    /// </summary>
    public UserSafety UserSafety { get; private set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreateDatetime { get; init; }

    /// <summary>
    /// 创建用户邮箱
    /// </summary>
    /// <param name="userRoleGuid">用户角色ID</param>
    /// <param name="userEmail">邮箱</param>
    /// <param name="passwordHash">密码哈希</param>
    /// <param name="imageCover">用户头像</param>
    /// <param name="authorGuids">用户作者ID</param>
    /// <returns>用户邮箱后的任务</returns>
    public static async Task<User> CreateByEmailUser(
        Guid userRoleGuid,
        string userEmail,
        string passwordHash,
        Uri? imageCover,
        HashSet<Guid>? authorGuids)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (string.IsNullOrEmpty(userEmail))
            throw new ArgumentNullException(nameof(userEmail), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ??
                    throw new ArgumentNullException("security stamp is null");

        var userGuid = Guid.CreateVersion7();
        var user = new User
        {
            UserGuid = userGuid,
            UserRoleGuid = [userRoleGuid],
            UserName = userEmail,
            UserEmail = userEmail,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            AvatarUrl = imageCover,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(userGuid) ??
                             throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(userGuid, stamp, Convert.ToBase64String(salt)) ??
                         throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow
        };

        user.AddDomainEvent(new UserStartedByEmailDomainEvent(user.UserGuid, userEmail, userRoleGuid, authorGuids));
        return user;
    }

    /// <summary>
    /// 创建用户手机号
    /// </summary>
    /// <param name="userRoleGuid">用户角色ID</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="passwordHash">密码哈希</param>
    /// <param name="imageCover">用户头像</param>
    /// <param name="authorGuids">用户作者ID</param>
    /// <returns>用户手机号后的任务</returns>
    public static async Task<User> CreateByPhoneUser(
        List<Guid> userRoleGuid,
        PhoneNumber phoneNumber,
        string passwordHash,
        Uri? imageCover,
        HashSet<Guid>? authorGuids)
    {
        if (userRoleGuid is null)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "Phone number cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ??
                    throw new ArgumentNullException("security stamp is null");

        var userGuid = Guid.CreateVersion7();
        var user = new User
        {
            UserGuid = userGuid,
            UserRoleGuid = userRoleGuid,
            UserName = phoneNumber.PhoneCode,
            UserEmail = phoneNumber.PhoneCode,
            PhoneNumber = phoneNumber,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            AvatarUrl = imageCover,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(userGuid) ??
                             throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(userGuid, stamp, Convert.ToBase64String(salt)) ??
                             throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow
        };

        user.AddDomainEvent(
            new UserStartedByPhoneDomainEvent(user.UserGuid, [..userRoleGuid], phoneNumber, authorGuids));
        return user;
    }

    /// <summary>
    /// 修改用户地址
    /// </summary>
    /// <param name="userAddress">新地址</param>
    public void ChangeByAddress(ref Address userAddress)
    {
        UserAddress = userAddress;
    }

    /// <summary>
    /// 更新用户头像（Identity 为头像唯一真相源；保存后发布事件同步 Message UserInfo）
    /// </summary>
    /// <param name="avatarUrl">新头像地址（FileDev 上传返回的 fileUri）</param>
    public void ChangeByAvatar(Uri avatarUrl)
    {
        if (avatarUrl is null)
            throw new ArgumentNullException(nameof(avatarUrl));
        if (avatarUrl.ToString().Length > 2048)
            throw new ArgumentException("头像地址不能超过2048个字符", nameof(avatarUrl));

        AvatarUrl = avatarUrl;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="password">新密码</param>
    /// <returns>修改密码后的任务</returns>
    public async ValueTask ChangeByPasswordAsync(string password)
    {
        if (UserSafety.UserStatus == UserStatus.Locked)
            throw new InvalidOperationException("用户已被锁定，无法修改密码");

        // 密码至少 8 位（< 8 拒绝；原 <= 8 会拒绝 8 位密码，与"至少 8 位"语义不符）
        if (password.Length < 8)
            throw new ArgumentException("密码长度不能小于 8 位", nameof(password));

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null!");
        var saltStr = Convert.ToBase64String(salt);

        UserSafety.ResetByPasswordSalt(saltStr);
        PasswordHash = await HashH256Tool.CreateHash256Async(password, Convert.FromBase64String(UserSafety.PasswordSalt!));
    }

    /// <summary>
    /// 绑定用户手机号
    /// </summary>
    /// <param name="region">手机号区域</param>
    /// <param name="phoneNumber">手机号</param>
    public void BandingByPhone(long region, string phoneNumber)
    {
        PhoneNumber = PhoneNumber.CreatePhoneNumber(region, phoneNumber) ??
                      throw new ArgumentNullException(nameof(PhoneNumber), "phone number is null");
        AddDomainEvent(new PhoneNumberBandingEvent(UserGuid, PhoneNumber.PhoneCode));
    }

    /// <summary>
    /// 验证用户手机号是否正确
    /// </summary>
    /// <param name="phoneNumber">用户输入的手机号</param>
    /// <returns>如果手机号正确则返回 true，否则返回 false</returns>
    public bool VerifyByPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
        return PhoneNumber?.PhoneCode == phoneNumber.PhoneCode &&
               PhoneNumber.AddressRegion == phoneNumber.AddressRegion;
    }

    /// <summary>
    /// 验证用户邮箱是否正确
    /// </summary>
    /// <param name="email">用户输入的邮箱</param>
    /// <returns>如果邮箱正确则返回 true，否则返回 false</returns>
    public bool VerifyByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentNullException(nameof(email), "email is null or empty");
        return UserEmail == email;
    }

    /// <summary>
    /// 验证用户密码是否正确
    /// </summary>
    /// <param name="password">用户输入的密码</param>
    /// <returns>如果密码正确则返回 true，否则返回 false</returns>
        public async Task<bool> VerifyByPasswordAsync(string password)
    {
        if (UserSafety is null)
            throw new InvalidOperationException("UserSafety is not loaded. Ensure the navigation property is included in the query.");

        var (isValid, needsRehash) = await CheckByPasswordAsync(password);

        if (isValid)
        {
            // S-13：旧迭代（100K）哈希验证通过后，自动用新迭代（600K）重哈希升级
            if (needsRehash)
                await RehashPasswordAsync(password);

            UserAccessFail.RecordSuccess();
        }
        // S-13：失败计数不再在此递增（非原子读改写），由 UserService 通过仓储 ExecuteUpdate 原子递增

        return isValid;
    }

    /// <summary>
    /// 使用新迭代（600K）重哈希密码并轮换盐（S-13 升级路径）
    /// </summary>
    public async ValueTask RehashPasswordAsync(string password)
    {
        if (UserSafety is null)
            throw new InvalidOperationException("UserSafety is not loaded.");

        var salt = await HashH256Tool.GenerateSValueTask()
                   ?? throw new ArgumentNullException("salt is null");
        var saltStr = Convert.ToBase64String(salt);

        UserSafety.ResetByPasswordSalt(saltStr);
        PasswordHash = await HashH256Tool.CreateHash256Async(
            password, Convert.FromBase64String(UserSafety.PasswordSalt!));
    }

    public void ChangeByEmail(
        [EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]
        string newEmail)
    {
        if (UserEmail == newEmail)
            throw new ArgumentException("需要不同的邮箱");

        UserEmail = newEmail;
    }

    /// <summary>
    /// 连接用户权限
    /// </summary>
    /// <param name="authorGuid">用户权限ID</param>
    public void LinkAuthority(Guid authorGuid)
    {
        if (authorGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(authorGuid), "authorGuid is empty");
        AuthorGuids.Add(authorGuid);
    }

    /// <summary>
    /// 断开用户权限
    /// </summary>
    /// <param name="authorGuid">用户权限ID</param>
    public void UnLinkAuthority(Guid authorGuid)
    {
        if (authorGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(authorGuid), "authorGuid is empty");
        AuthorGuids.Remove(authorGuid);
    }

    /// <summary>
    /// 验证用户密码是否正确
    /// </summary>
    /// <param name="password">用户输入的密码</param>
    /// <returns>如果密码正确则返回 true，否则返回 false</returns>
        private async Task<(bool Valid, bool NeedsRehash)> CheckByPasswordAsync(string password)
    {
        var salt = UserSafety.PasswordSalt ??
                   throw new InvalidOperationException("Password salt is not set");

        return await HashH256Tool.VerifyPasswordWithUpgradeAsync(
            password,
            PasswordHash,
            Convert.FromBase64String(salt));
    }
}