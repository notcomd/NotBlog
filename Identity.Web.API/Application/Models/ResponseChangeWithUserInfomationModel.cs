namespace Identity.Web.API.Application.Models
{
    public sealed class ResponseChangeWithUserInfomationModel
    {
        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public PhoneNumber? PhoneNumber { get; set; }

        public string Address { get; set; }

        public Uri ImageUri { get; set; }
    }
}
