namespace Identity.Web.API.Application.Models
{
    public sealed class RequestChangeWithUserInfomationModel
    {

        public RequestChangeWithUserInfomationModel(string userEmail, string userName, string address, Uri imageUri)
        {
            UserName = userName;
            Address = address;
            ImageUri = imageUri;
            UserEmail = userEmail;
        }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string Address { get; set; }

        public Uri ImageUri { get; set; }
    }
}
