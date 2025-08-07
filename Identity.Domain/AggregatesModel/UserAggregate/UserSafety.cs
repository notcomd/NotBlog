namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserSafety : Entity
{
       

    public Guid UserGuid { get; private set; }

    public string SecurityStamp { get; private set; } = null!;

    public string PasswordSalt { get; private set; } = null!;

    public EnumBlackOrWhite BlackOrWhite { get; private set; }

    public EnumUserStatus UserStatus { get; private set; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    public bool IsDeleted { get; private set; } = false;

    public bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;



    public static ValueTask<UserSafety> CreateByUserSafety(Guid UserGuid, string passwordSalt, string securityStamp, 
        EnumBlackOrWhite blackOrWhite = EnumBlackOrWhite.AuthorityWhite, EnumUserStatus userStatus = EnumUserStatus.Normal)
    {

        if (UserGuid != Guid.Empty)
        {
            if (string.IsNullOrEmpty(passwordSalt) && string.IsNullOrEmpty(securityStamp))
                throw new ArgumentException("At least one of passwordSalt or securityStamp must be provided", nameof(passwordSalt));

            var userSafety = new UserSafety
            {                       
                Id =Guid.CreateVersion7(),
                UserGuid=UserGuid,
                SecurityStamp = securityStamp,
                PasswordSalt = passwordSalt,
                BlackOrWhite = blackOrWhite,
                UserStatus = userStatus
            };

            return new ValueTask<UserSafety>(userSafety);
        }

        throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");
    }

    /// <summary>
    /// 修改用户锁定状态
    /// </summary>
    /// <param name="lockOutEnd"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetOrChangeByLockOutEnd(DateTimeOffset? lockOutEnd)
    {
        if (IsLockedOut)
            throw new ArgumentException("LockOutEnd cannot be in the past", nameof(lockOutEnd));
        LockOutEnd = lockOutEnd;

    }

    public void SetOrResetByPasswordSalt(string newPasswordSalt)
    {
        if (string.IsNullOrWhiteSpace(newPasswordSalt))
            throw new ArgumentException("Password salt cannot be null or empty", nameof(newPasswordSalt));
        PasswordSalt = newPasswordSalt;
    }

    public void SetOrResetBySecurityStamp(string newSecurityStamp)
    {
        if (string.IsNullOrWhiteSpace(newSecurityStamp))
            throw new ArgumentException("Security stamp cannot be null or empty", nameof(newSecurityStamp));
        SecurityStamp = newSecurityStamp;
    }

    public void SetOrResetByIsDeleted(bool isDeleted)
    {
        if (isDeleted == IsDeleted)
            throw new ArgumentException("IsDeleted state is already set to the same value", nameof(isDeleted));

        IsDeleted = isDeleted;
    }

    public void SetOrResetByLockTime(DateTimeOffset? dateTimeOffset)
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

    public void SetOrResetByBlackOrWhiteAndUserStatus(EnumBlackOrWhite blackOrWhite, EnumUserStatus enumUserStatus)
    {
        if (blackOrWhite == BlackOrWhite && enumUserStatus == UserStatus)
            throw new ArgumentException("BlackOrWhite and UserStatus are already set to the same values", nameof(blackOrWhite));
        BlackOrWhite = blackOrWhite;
        UserStatus = enumUserStatus;
    }

        

}