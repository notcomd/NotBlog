namespace Identity.Web.API.Application.Models
{
    public class RequestRegisterWithGenerateeCodeModel
    {
        public RequestRegisterWithGenerateeCodeModel(string registerEmail)
        {
            RegisterEmail = registerEmail;
        }

        public string RegisterEmail { get; set; }
    }
}
