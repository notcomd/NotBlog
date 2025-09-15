namespace Identity.Web.API.Application.Models
{
    public sealed class ResponseChangeWithUserInfomationModel
    {
        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public PhoneNumber? PhoneNumber { get; set; }

        public string Address { get; set; }

        public Uri ImageUri { get; set; }
<<<<<<< HEAD

        public ResponseChangeWithUserInfomationModel(string userName, string userEmail, PhoneNumber? phoneNumber, string address, Uri imageUri)
        {
            UserName = userName;
            UserEmail = userEmail;
            PhoneNumber = phoneNumber;
            Address = address;
            ImageUri = imageUri;
        }
=======
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
    }
}
