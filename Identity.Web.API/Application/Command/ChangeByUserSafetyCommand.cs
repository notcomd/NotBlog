namespace Identity.Web.API.Application.Command
{
<<<<<<< HEAD
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
=======
    public class ChangeByUserSafetyCommand
    {
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
    }
}
