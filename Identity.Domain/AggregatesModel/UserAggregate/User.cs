<<<<<<< HEAD
﻿
=======
﻿using Identity.Domain.Events;
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9

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

    private readonly List<UserClaim> _UserClaims = new();

    public IReadOnlyCollection<UserClaim?> UserClaims => _UserClaims.AsReadOnly();

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
    public User(Guid userRoleGuid, string userEmail, string passwordHash,
        DateTimeOffset dateTimeOffset)
    {
        if (userRoleGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userRoleGuid), "User role cannot be null or empty");
        if (string.IsNullOrEmpty(userEmail))
            throw new ArgumentNullException(nameof(userEmail), "User email cannot be null or empty");
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null or empty");

        var stamp = HashHelper.GenerateSecurityStamp();
        var salt = HashHelper.GenerateSaltValue();
        passwordHash = HashHelper.CreateHash256Async(passwordHash, salt
            ?? throw new ArgumentNullException("salt is null!"));

        Id = Guid.CreateVersion7();
        UserRoleGuid = userRoleGuid;
        UserName = userEmail;
        PasswordHash = passwordHash;
        CreateDatetime = dateTimeOffset;
        UserEmail = userEmail;
        ImageCover = null;
        PhoneNumber = null;
        this.UserAccessFail = UserAccessFail.CreateByUserAccessFail(Id);
        UserSafety = UserSafety.CreateByUserSafety(Id, Encoding.UTF8.GetString(salt), stamp);
        AddDomainEvent(new CreatedByUserDomainEvent(Id, userRoleGuid, userEmail, userEmail, null, dateTimeOffset));

<<<<<<< HEAD
=======
        Id = Guid.CreateVersion7();
        UserRoleGuid = userRoleGuid;
        UserName = userEmail;
        PasswordHash = passwordHash;
        CreateDatetime = dateTimeOffset;
        UserEmail = userEmail;
        ImageCover = null;
        PhoneNumber = null;
        UserAccessFail = new UserAccessFail(this);
        UserSafety = UserSafety.CreateByUserSafety(Id, Encoding.UTF8.GetString(salt), stamp);
        AddDomainEvent(new CreatedByUserDomainEvent(Id, userRoleGuid, userEmail, userEmail, null, dateTimeOffset));

>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
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

        var stamp = HashHelper.GenerateSecurityStamp();
        var salt = HashHelper.GenerateToString(HashHelper.GenerateSaltValue());

<<<<<<< HEAD
        passwordHash = HashHelper.CreateHash256Async(passwordHash, HashHelper.ConvertStringToBytes(salt)
            ?? throw new ArgumentNullException("salt is null!"));

=======
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9

        Id = Guid.CreateVersion7();
        UserRoleGuid = userRoleGuid;
        UserName = phoneNumber.PhoneCode;
        PasswordHash = passwordHash;
<<<<<<< HEAD
        CreateDatetime = dateTimeOffset;
=======
        CreateDatetime = dateTimeOffset;       
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
        UserEmail = string.Empty;
        ImageCover = null;
        PhoneNumber = phoneNumber;
        UserAccessFail = UserAccessFail.CreateByUserAccessFail(Id);
<<<<<<< HEAD
        UserSafety = UserSafety.CreateByUserSafety(Id, salt, stamp);

=======
        UserSafety = UserSafety.CreateByUserSafety(Id, Encoding.UTF8.GetString(salt), stamp);
        
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
        AddDomainEvent(new CreatedByUserDomainEvent(Id, userRoleGuid, phoneNumber.PhoneCode, string.Empty, phoneNumber, DateTimeOffset.UtcNow));
        //return UserResult;
    }

    /// <summary>
    /// 重新设置用户名
    /// </summary>
    /// <param name="userName"> 用户名</param>
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
    /// <param name="imageCover">图片地址</param>
    /// <exception cref="ArgumentNullException"> 图片地址不能为空</exception>
    /// <exception cref="ArgumentException"> 图片地址必须是绝对地址</exception>
    public void ChangeByImageCover(Uri imageCover)
    {
        ArgumentNullException.ThrowIfNull(imageCover, nameof(imageCover));

        if (!imageCover.IsAbsoluteUri)
        {
            throw new ArgumentException("头像地址必须是绝对路径", nameof(imageCover));
        }
        if (ImageCover?.Equals(imageCover) == true)
        {
            throw new ArgumentException("新头像不能与当前头像相同");
        }
        ImageCover = imageCover;
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
    public void ChangeByPassword(string password, byte[] salt, string stamp)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentNullException.ThrowIfNull(salt, nameof(salt));
        if (string.IsNullOrEmpty(stamp))
        {
            throw new ArgumentNullException(nameof(stamp), "security stamp is null or empty");
        }
        if (UserSafety.UserStatus == EnUserStatus.Locked || UserSafety.BlackOrWhite == EnBlackOrWhite.AuthorityBlack)
        {
            throw new InvalidOperationException("用户已被锁定，无法修改密码");
        }
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException(nameof(password));
        }
        //var str = Encoding.UTF8.GetString(salt);
        PasswordHash = HashHelper.CreateHash256Async(password, salt
            ?? throw new ArgumentNullException(nameof(salt)));
    }

    /// <summary>
    /// 绑定手机号
    /// </summary>
    /// <param name="region">手机区号</param>
    /// <param name="phoneNumber">电话号码</param>
    /// <returns>
    /// 手机对象
    /// </returns>
    public void BendingByPhone(long region, string phoneNumber)
    {
        PhoneNumber = PhoneNumber.CreatePhoneNumber(Id, region, phoneNumber)
        ?? throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
    }

    /// <summary>
    /// 验证手机号是否正确
    /// </summary>
    /// <param name="phoneNumber"> 手机号对象</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"> 手机号对象不能为空</exception>
    /// <exception cref="ArgumentException"> 手机号格式错误</exception>
    public bool IsVerifyByPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
        if (string.IsNullOrEmpty(phoneNumber.PhoneCode))
            throw new ArgumentNullException(nameof(phoneNumber), "phone number code is null or empty");
        switch (phoneNumber.AddressRegion)
        {
            case (long)EnAddressRegion.China:
            case (long)EnAddressRegion.UnitedStates:
            case (long)EnAddressRegion.SouthKorea:
            case (long)EnAddressRegion.Japan:
            case (long)EnAddressRegion.Taiwan:
            case (long)EnAddressRegion.Hongkong:
            case (long)EnAddressRegion.Singapore:
            case (long)EnAddressRegion.XiaMen:
                break;
            default:
                throw new ArgumentException("phone number address region is error", nameof(phoneNumber));
        }

        return PhoneNumber?.PhoneCode == phoneNumber.PhoneCode && PhoneNumber.AddressRegion == phoneNumber.AddressRegion;
    }

    /// <summary>
    /// 验证邮箱是否正确
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"> 邮箱不能为空</exception>
    /// <exception cref="ArgumentException"> 邮箱格式错误</exception>
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
<<<<<<< HEAD
    /// <param name="passwordHash"> 密码哈希值</param>
    /// <returns> 验证结果</returns>
    public bool IsVerifyByPassword(string passwordHash)
    {
        if (string.IsNullOrEmpty(UserSafety.PasswordSalt))
            throw new AggregateException("passwordSalt is null");
        return HashHelper.VerifyPasswordValueTask(passwordHash, PasswordHash, HashHelper.ConvertStringToBytes(UserSafety.PasswordSalt));
    }
=======
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    public ValueTask<bool> IsVerifyByPasswordAsync(string passwordHash) => HashHelper.VerifyPasswordValueTask(passwordHash, PasswordHash, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9

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
    /// 更新用户地址信息
    /// </summary>
    /// <param name="address">新地址</param>
    /// <exception cref="ArgumentException">当地址为空或超过长度限制时抛出</exception>
    public void ChangeByAddress(string address)
    {
<<<<<<< HEAD
        // 验证地址是否为空或仅包含空白字符
        ArgumentException.ThrowIfNullOrWhiteSpace(address, nameof(address));
        // 验证地址长度
        const int MaxAddressLength = 100;
        if (address.Length > MaxAddressLength)
        {
            throw new ArgumentException($"地址长度不能超过{MaxAddressLength}个字符", nameof(address));
        }
        // 检查是否需要更新
        if (Address == address)
        {
            return;
        }
        // 更新地址
        Address = address.Trim();
=======
        if (string.IsNullOrEmpty(address))
            throw new ArgumentException("地址不能为空", nameof(address));

        if (address.Length > 100)
            throw new ArgumentException("地址长度不能超过100个字符", nameof(address));

        Address = address;
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
    }

    /// <summary>
    /// 添加用户声明
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <param name="claimValue">声明值</param>
    /// <exception cref="ArgumentException">当用户已存在相同类型声明时抛出</exception>
    public void AddUserClaim(string claimType, string claimValue)
    {

        if (_UserClaims.Any(c => c.ClaimType == claimType))
        {
            throw new ArgumentException($"User already has a claim of type {claimType}", nameof(claimType));
        }
        var newClaim = UserClaim.CreateByUserClaim(Id, claimType, claimValue);
        _UserClaims.Add(newClaim);
    }


    /// <summary>
    /// 更新用户声明
    /// </summary>
    /// <param name="type">声明类型</param>
    /// <param name="newValue">新声明值</param>
    /// <exception cref="ArgumentException">当用户不存在该类型声明时抛出</exception>
    public void UpdateClaim(string type, string newValue)
    {
        var claim = _UserClaims.FirstOrDefault(c => c.ClaimType == type)
            ?? throw new ArgumentException($"User does not have a claim of type {type}", nameof(type));
        claim.UpdateClaim(type, newValue);
    }

    /// <summary>
    /// 删除用户声明
    /// </summary>
    /// <param name="type">声明类型</param>
    /// <exception cref="ArgumentException">当用户不存在该类型声明时抛出</exception>
    public void RemoveClaim(string type)
    {
        var claim = _UserClaims.FirstOrDefault(c => c.ClaimType == type);
        if (claim is null)
        {
            throw new ArgumentException($"User does not have a claim of type {type}", nameof(type));
        }
        _UserClaims.Remove(claim);
    }


    /// <summary>
    /// 将用户声明转换为Claim对象
    /// </summary>
    /// <param name="userClaims">用户声明集合</param>
    /// <returns>Claim对象集合</returns>
    /// <exception cref="ArgumentNullException">当用户声明集合为空时抛出</exception>
    public IEnumerable<Claim>? UserClaimToClaim(IEnumerable<UserClaim> userClaims)
    {

<<<<<<< HEAD
        if (userClaims is null || !userClaims.Any())
            throw new ArgumentNullException(nameof(userClaims), "user claims is null or empty");

        return userClaims.Where(entity => string.IsNullOrEmpty(entity.ClaimValue) || string.IsNullOrEmpty(entity.ClaimType))
             .Select(en => en.ToClaim());

    }

    /// <summary>
    /// 更新用户声明
    /// </summary>
    /// <param name="userClaims">新的用户声明集合</param>
    /// <exception cref="ArgumentNullException">当用户声明集合为空时抛出</exception>
    public void ChangeByUserClaim(IEnumerable<UserClaim> userClaims)
    {
        if (userClaims is null || !userClaims.Any())
            throw new ArgumentNullException(nameof(userClaims), "user claims is null or empty");
        _UserClaims.Clear();
        _UserClaims.AddRange(userClaims);
    }

    /// <summary>
    /// 检查用户是否被锁定
    /// </summary>
    /// <returns>如果用户被锁定则返回true，否则返回false</returns>
=======
        if (userClaims is not null && userClaims.Any())
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

>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
    public bool IsUserLockedOut() => UserAccessFail.IsLockOutByAccessFaild();

}