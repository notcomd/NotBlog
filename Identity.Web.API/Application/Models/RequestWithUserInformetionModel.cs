namespace Identity.Web.API.Application.Models
{
    public class RequestWithUserInformetionModel
    {
        public RequestWithUserInformetionModel(string findByEmail)
        {
            FindByEmail = findByEmail;
        }

        public string FindByEmail { get; }
    }
}
