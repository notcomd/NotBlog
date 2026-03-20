using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class User : Entity, IAggregateRoot
{
    protected User()
    {
        UserGuid = Guid.CreateVersion7();
        UserRoleGuid = new HashSet<Guid>();
        AuthorGuids = new HashSet<Guid>();
        UserAccessFail = UserAccessFail.CreateUserAccessFail(UserGuid) ??
                         throw new ArgumentNullException(nameof(UserAccessFail));
        CreateDatetime = DateTimeOffset.UtcNow;
    }

    public Guid UserGuid { get; init; }

    public HashSet<Guid> UserRoleGuid { get; private set; }

    public HashSet<Guid> AuthorGuids { get; private set; }
    
    public string? UserName { get; private set; }

    public Uri ImageCover { get; private set; }

    public string UserEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    public Address? UserAddress { get; private set; }

    public UserAccessFail UserAccessFail { get; private set; }

    public UserSafety UserSafety { get; private set; }

    public DateTimeOffset CreateDatetime { get; init; }

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
        if (imageCover == null)
            throw new ArgumentNullException(nameof(imageCover), "Image cover cannot be null");

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ??
                    throw new ArgumentNullException("security stamp is null");
        
        var user = new User
        {
            UserRoleGuid = [userRoleGuid],
            UserName = userEmail,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            ImageCover = imageCover,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(Guid.CreateVersion7()) ??
                             throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(Guid.CreateVersion7(), stamp, salt.ToString()) ??
                         throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow
        };
        
        user.AddDomainEvent(new UserStartedByEmailDomainEvent([userRoleGuid], userEmail, passwordHash, imageCover, authorGuids));
        return user;
    }

    public static async Task<User> CreateByPhoneUser(
        HashSet<Guid> userRoleGuid, 
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
        if (imageCover == null)
            throw new ArgumentNullException(nameof(imageCover), "Image cover cannot be null");

        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null");
        var stamp = await JwtRandom.GenerateSecurityStamp() ??
                    throw new ArgumentNullException("security stamp is null");
        
        var user = new User
        {
            UserGuid = Guid.CreateVersion7(),
            UserRoleGuid = userRoleGuid,
            PhoneNumber = phoneNumber,
            PasswordHash = await HashH256Tool.CreateHash256Async(passwordHash, salt),
            ImageCover = imageCover,
            UserAccessFail = UserAccessFail.CreateUserAccessFail(Guid.CreateVersion7()) ??
                             throw new ArgumentNullException(nameof(UserAccessFail)),
            UserSafety = UserSafety.CreateByUserSafety(Guid.CreateVersion7(), stamp, salt.ToString()) ??
                         throw new ArgumentNullException(nameof(UserSafety)),
            CreateDatetime = DateTimeOffset.UtcNow
        };
        
        user.AddDomainEvent(
            new UserStartedByPhoneDomainEvent(userRoleGuid, phoneNumber, passwordHash, imageCover, authorGuids));
        return user;
    }

    public void ChangeByAddress(ref Address userAddress)
    {
        UserAddress = userAddress;
    }

    public async ValueTask ChangeByPasswordAsync(string password)
    {
        if (UserSafety.UserStatus == UserStatus.Locked) 
            throw new InvalidOperationException("用户已被锁定，无法修改密码");
        
        if (password.Length <= 8) 
            throw new ArgumentOutOfRangeException(nameof(password), "密码长度不能小于 8 位");
        
        var salt = await HashH256Tool.GenerateSValueTask() ?? throw new ArgumentNullException("salt is null!");
        var saltStr = salt.ToString() ?? throw new ArgumentNullException(nameof(salt), "Salt is null");
        
        UserSafety.ResetByPasswordSalt(saltStr);
        PasswordHash = await HashH256Tool.CreateHash256Async(password, Encoding.UTF8.GetBytes(UserSafety.PasswordSalt));
    }

    public void BandingByPhone(long region, string phoneNumber)
    {
        PhoneNumber = PhoneNumber.CreatePhoneNumber(region, phoneNumber) ??
                      throw new ArgumentNullException(nameof(PhoneNumber), "phone number is null");
        AddDomainEvent(new PhoneNumberBandingEvent(UserGuid, PhoneNumber.PhoneCode));
    }

    public bool VerifyByPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException(nameof(phoneNumber), "phone number is null");
        return PhoneNumber?.PhoneCode == phoneNumber.PhoneCode &&
               PhoneNumber.AddressRegion == phoneNumber.AddressRegion;
    }

    public bool VerifyByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentNullException(nameof(email), "email is null or empty");
        return UserEmail == email;
    }

    public async Task<bool> VerifyByPasswordAsync(string password)
    {
        var isValid = await CheckByPasswordAsync(password);
        
        if (!isValid)
        {
            UserAccessFail.VerifyByAccessFaild(true);
            AddDomainEvent(new AccountLockedEvent(UserGuid));
        }

        return isValid;
    }

    public void ChangeByEmail(
        [EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]
        string newEmail)
    {
        if (UserEmail == newEmail)
            throw new ArgumentException("需要不同的邮箱");
        
        UserEmail = newEmail;
    }

    public void LinkAuthority(Guid authorGuid)
    {
        if (authorGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(authorGuid), "authorGuid is empty");
        AuthorGuids.Add(authorGuid);
    }
    
    public void UnLinkAuthority(Guid authorGuid)
    {
        if (authorGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(authorGuid), "authorGuid is empty");
        AuthorGuids.Remove(authorGuid);
    }

    // public void AddExternalLogin(UserExternalLogin externalLogin)
    // {
    //     if (_externalLogins.Any(e => e.LoginProvider == externalLogin.LoginProvider && 
    //                                   e.ProviderKey == externalLogin.ProviderKey))
    //         throw new InvalidOperationException("External login already exists");
    //     
    //     _externalLogins.Add(externalLogin);
    //     AddDomainEvent(new ExternalLoginAddedEvent(UserGuid, externalLogin.LoginProvider));
    // }
    //
    // public bool HasExternalLogin(string provider, string providerKey)
    // {
    //     return _externalLogins.Any(e => e.LoginProvider == provider && e.ProviderKey == providerKey);
    // }

    private async Task<bool> CheckByPasswordAsync(string password)
    {
        var salt = UserSafety.PasswordSalt ??
                   throw new InvalidOperationException("Password salt is not set");
        
        return await HashH256Tool.VerifyPasswordValueTask(
            password, 
            PasswordHash, 
            Encoding.UTF8.GetBytes(salt));
    }
}