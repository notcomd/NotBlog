namespace Identity.Web.API.Application.Command
{
    public class ChangeByPasswodCommand : IRequest<bool>
    {
        public string UserEmail { get; }
        public string OldPassword { get; }
        public string NewPassword { get; }

        public ChangeByPasswodCommand(string userEmail, string oldPassword, string newPassword)
        {
            UserEmail = userEmail ?? throw new ArgumentNullException(nameof(userEmail));
            OldPassword = oldPassword ?? throw new ArgumentNullException(nameof(oldPassword));
            NewPassword = newPassword ?? throw new ArgumentNullException(nameof(newPassword));
        }
    }
}
