namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserSafety : Entity
{

    public Guid UserGuid { get; private set; }

    public string SecurityStamp { get; private set; } = null!;

    public string PasswordSalt { get; private set; } = null!;

    public EnBlackOrWhite BlackOrWhite { get; private set; }

    public EnUserStatus UserStatus { get; private set; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    private bool IsDeleted => UserStatus == EnUserStatus.Deleted;

    private bool IsActive => UserStatus != EnUserStatus.UnActive || UserStatus != EnUserStatus.Deleted;

    private bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;

    protected UserSafety()
    {
        Id = Guid.CreateVersion7();
    }

    public static UserSafety CreateByUserSafety(Guid UserGuid, string passwordSalt, string securityStamp)
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
                BlackOrWhite = EnBlackOrWhite.AuthorityWhite,
                UserStatus = EnUserStatus.UnActive,
                LockOutEnd = null,
            };

            return userSafety;
        }

        throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");
    }

    public UserSafety(User user, string passwordSalt, string securityStamp,
        EnBlackOrWhite blackOrWhite = EnBlackOrWhite.AuthorityWhite, EnUserStatus userStatus = EnUserStatus.UnActive)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user), "User cannot be null");
        if (string.IsNullOrEmpty(passwordSalt) && string.IsNullOrEmpty(securityStamp))
            throw new ArgumentException("At least one of passwordSalt or securityStamp must be provided", nameof(passwordSalt));
        UserGuid = user.Id;
        SecurityStamp = securityStamp;
        PasswordSalt = passwordSalt;
        BlackOrWhite = blackOrWhite;
        UserStatus = userStatus;
        LockOutEnd = null;
    }

    public void ChangeByPasswordSalt(string newPasswordSalt)
    {
        if (string.IsNullOrWhiteSpace(newPasswordSalt))
        {
            throw new ArgumentException("Password salt cannot be null or empty", nameof(newPasswordSalt));
        }

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

        LockOutEnd = dateTimeOffset;
    }

    public void ChangeByBlackOrWhiteStatus(EnBlackOrWhite? blackOrWhite)
    {
        if (blackOrWhite == BlackOrWhite || blackOrWhite is null)
            return;
        BlackOrWhite = (EnBlackOrWhite)blackOrWhite;
    }

    public void ChangeByUserStatus(EnUserStatus? userStatus)
    {
        if (userStatus == UserStatus || userStatus is null)
            return;
        UserStatus = (EnUserStatus)userStatus;
    }

    public bool GetIsActive() => IsActive;

    public bool GetIsLockedOut()
    {
        if (IsLockedOut)
            return true;

        LockOutEnd = null;
        return false;
    }

    public bool GetIsDeleted() => IsDeleted;

    public void ResetLockout()
    {
        LockOutEnd = null;
    }
}