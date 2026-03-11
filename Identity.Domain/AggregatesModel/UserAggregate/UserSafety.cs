namespace Identity.Domain.AggregatesModel.UserAggregate
{
    public class UserSafety : Entity
    {

        public Guid UserSafetyGuid { get; init; }

        public Guid UserGuid { get; private set; }

        public string? SecurityStamp { get; private set; }

        public string? PasswordSalt { get; private set; }

        public BlackOrWhite BlackOrWhite { get; private set; }

        public UserStatus UserStatus { get; private set; }

        public DateTimeOffset? LockOutEnd { get; private set; }

        public bool IsLockedOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;

        private UserSafety()
        {
            this.UserSafetyGuid = Guid.CreateVersion7();
            this.BlackOrWhite = BlackOrWhite.AuthorityWhite;
            this.UserStatus = UserStatus.Normal;
        }

        public UserSafety(User user, string securityStamp, string passwordSalt) : this()
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));
            if (securityStamp == null)
                throw new ArgumentNullException(nameof(securityStamp));
            this.SecurityStamp = securityStamp;
            this.PasswordSalt = passwordSalt;
            this.UserGuid = Guid.NewGuid();
        }

        public static UserSafety CreateByUserSafety(Guid UserGuid, string? securityStamp, string? passwordSalt,
            BlackOrWhite blackOrWhite = BlackOrWhite.AuthorityWhite, UserStatus userStatus = UserStatus.Normal)
        {

            if (UserGuid == null)
                throw new ArgumentNullException(nameof(UserGuid), "User cannot be null");

            if (string.IsNullOrEmpty(passwordSalt) && string.IsNullOrEmpty(securityStamp))
                throw new ArgumentException("At least one of passwordSalt or securityStamp must be provided", nameof(passwordSalt));

            var userSafety = new UserSafety
            {
                UserSafetyGuid = Guid.CreateVersion7(),
                //User = user,
                UserGuid = UserGuid,
                SecurityStamp = securityStamp,
                PasswordSalt = passwordSalt,
                BlackOrWhite = blackOrWhite,
                UserStatus = userStatus
            };

            return userSafety;
        }

        /// <summary>
        /// 修改用户锁定状态
        /// </summary>
        /// <param name="lockOutEnd"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ChangeByLockOutEnd(DateTimeOffset? lockOutEnd)
        {
            if (IsLockedOut)
                throw new ArgumentException("LockOutEnd cannot be in the past", nameof(lockOutEnd));
            LockOutEnd = lockOutEnd;

        }

        public void ResetByPasswordSalt(string newPasswordSalt)
        {
            if (string.IsNullOrWhiteSpace(newPasswordSalt))
                throw new ArgumentException("Password salt cannot be null or empty", nameof(newPasswordSalt));
            PasswordSalt = newPasswordSalt;
        }

        public void ResetBySecurityStamp(string newSecurityStamp)
        {
            if (string.IsNullOrWhiteSpace(newSecurityStamp))
                throw new ArgumentException("Security stamp cannot be null or empty", nameof(newSecurityStamp));
            SecurityStamp = newSecurityStamp;
        }
    }
}