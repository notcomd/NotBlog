namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserSafety : Entity
{

    public Guid UserGuid { get; private set; }

    public string SecurityStamp { get; private set; } = null!;

    public string PasswordSalt { get; private set; } = null!;

    public EnumBlackOrWhite BlackOrWhite { get; private set; }

    public EnumUserStatus UserStatus { get; private set; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    private bool IsDeleted => UserStatus == EnumUserStatus.Deleted;

    private bool IsActive => UserStatus != EnumUserStatus.UnActive || UserStatus != EnumUserStatus.Deleted;

    private bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;

    protected UserSafety()
    {
        Id = Guid.CreateVersion7();
    }

    public static UserSafety CreateByUserSafety(Guid UserGuid, string passwordSalt, string securityStamp,
        EnumBlackOrWhite blackOrWhite = EnumBlackOrWhite.AuthorityWhite, EnumUserStatus userStatus = EnumUserStatus.UnActive)
    {

        if (UserGuid != Guid.Empty)
        {
            if (string.IsNullOrEmpty(passwordSalt) && string.IsNullOrEmpty(securityStamp))
                throw new ArgumentException("At least one of passwordSalt or securityStamp must be provided", nameof(passwordSalt));

            var userSafety = new UserSafety
            {
                UserGuid = UserGuid,
                SecurityStamp = securityStamp,
                PasswordSalt = passwordSalt,
                BlackOrWhite = blackOrWhite,
                UserStatus = userStatus,
                LockOutEnd = null,                
            };

            return userSafety;
        }

        throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");
    }
      

    public void ChangeByPasswordSalt(string newPasswordSalt)
    {
        if (string.IsNullOrWhiteSpace(newPasswordSalt))
            throw new ArgumentException("Password salt cannot be null or empty", nameof(newPasswordSalt));
        PasswordSalt = newPasswordSalt;
    }

    public void ChangeBySecurityStamp(string newSecurityStamp)
    {
        if (string.IsNullOrWhiteSpace(newSecurityStamp))
            throw new ArgumentException("Security stamp cannot be null or empty", nameof(newSecurityStamp));
        SecurityStamp = newSecurityStamp;
    }

    public void ChangeByLockOutEndTime(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset is null)
        {
            LockOutEnd = null;
            return;
        }
        if (dateTimeOffset.Value == default)
            throw new ArgumentException("DateTimeOffset cannot be default", nameof(dateTimeOffset));
        if (dateTimeOffset.Value < DateTimeOffset.UtcNow)
            throw new ArgumentException("DateTimeOffset cannot be in the future", nameof(dateTimeOffset));

        LockOutEnd = dateTimeOffset.Value;
    }

    public void ChangeByBlackOrWhiteStatus(EnumBlackOrWhite blackOrWhite)
    {
        if (blackOrWhite == BlackOrWhite)
            throw new ArgumentException("BlackOrWhite is already set to the same value", nameof(blackOrWhite));
        BlackOrWhite = blackOrWhite;
    }

    public void ChangeByUserStatus(EnumUserStatus userStatus)
    {
        if (userStatus == UserStatus)
            throw new ArgumentException("UserStatus is already set to the same value", nameof(userStatus));
        UserStatus = userStatus;
    }
       
    public bool GetIsActive() => IsActive;

    public bool GetIsLockedOut() => IsLockedOut;

    public bool GetIsDeleted() => IsDeleted;

}