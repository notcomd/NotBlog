namespace Identity.Web.API.Application.Models
{
    public class WithResultPhone
    {
        public WithResultPhone(long addressRegion, string phoneCode)
        {
            AddressRegion = addressRegion;
            PhoneCode = phoneCode;
        }

        public long AddressRegion { get; }

        public string PhoneCode { get; }
    }
}
