namespace Identity.Web.API.Application.Command
{
    public class ChangeByUserSafetyCommand : IRequest<bool>
    {
        public ChangeByUserSafetyCommand(string userEmail, EnBlackOrWhite enumBlackOrWhite, EnUserStatus enumUserStatus)
        {
            UserEmail = userEmail;
            EnumBlackOrWhite = enumBlackOrWhite;
            EnumUserStatus = enumUserStatus;
        }

        public string UserEmail { get; }

        public EnBlackOrWhite EnumBlackOrWhite { get; }

        public EnUserStatus EnumUserStatus { get; }
    }
}
