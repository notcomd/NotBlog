namespace Identity.Web.API.Application.Models
{
    public class RequestChangeWithPassworrdInformetionModel
    {
        public RequestChangeWithPassworrdInformetionModel(string userEmail, string oldPassword, string newPassword)
        {
            UserEmail = userEmail;
            OldPassword = oldPassword;
            NewPassword = newPassword;
        }

        public string UserEmail { get; } = string.Empty;
        public string OldPassword { get;  } = string.Empty;
        public string NewPassword { get; } = string.Empty;

        
    }
}
