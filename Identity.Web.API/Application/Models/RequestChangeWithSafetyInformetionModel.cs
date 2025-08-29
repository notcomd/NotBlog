namespace Identity.Web.API.Application.Models
{
    public class RequestChangeWithSafetyInformetionModel
    {
        public RequestChangeWithSafetyInformetionModel(string userEmail, string? securityStamp, string? passwordSalt,
            EnBlackOrWhite? blackOrWhite, EnUserStatus? userStatus, DateTimeOffset? lockOutEnd)
        {
            UserEmail = userEmail;
            SecurityStamp = securityStamp;
            PasswordSalt = passwordSalt;
            BlackOrWhite = blackOrWhite;
            UserStatus = userStatus;
            LockOutEnd = lockOutEnd;
        }
        public string UserEmail { get; }
        public string? SecurityStamp { get; }
        public string? PasswordSalt { get; }
        public EnBlackOrWhite? BlackOrWhite { get; }
        public EnUserStatus? UserStatus { get; }
        public DateTimeOffset? LockOutEnd { get; }
    }
}
