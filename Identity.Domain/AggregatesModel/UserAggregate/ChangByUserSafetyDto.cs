namespace Identity.Domain.AggregatesModel.UserAggregate;

public class ChangByUserSafetyDto
{
    public ChangByUserSafetyDto(string? securityStamp, string? passwordSalt, 
        EnBlackOrWhite? blackOrWhite, EnUserStatus? userStatus, DateTimeOffset? lockOutEnd)
    {
        SecurityStamp = securityStamp;
        PasswordSalt = passwordSalt;
        BlackOrWhite = blackOrWhite;
        UserStatus = userStatus;
        LockOutEnd = lockOutEnd;
    }

    public string? SecurityStamp { get; }

    public string? PasswordSalt { get; }

    public EnBlackOrWhite? BlackOrWhite { get; }

    public EnUserStatus? UserStatus { get; }

    public DateTimeOffset? LockOutEnd { get; }
}
