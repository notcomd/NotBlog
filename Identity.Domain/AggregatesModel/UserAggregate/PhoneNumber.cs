using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace Identity.Domain.Entities;

public class PhoneNumber : Entity
{

    protected PhoneNumber() { } // EF Core needs a parameterless constructor

    public Guid UserGuid { get; private set; }

    public long AddressRegion { get; set; }

    public string PhoneCode { get; set; } = null!;
        

    public static PhoneNumber CreatePhoneNumber(Guid userGuid,long addressRegion, string phoneCode)
    {
        if(userGuid == Guid.Empty)
            throw new ArgumentNullException(nameof(userGuid));
        if (string.IsNullOrWhiteSpace(phoneCode) || (phoneCode.Length <= 11 && phoneCode.Length >= 8))
            throw new ArgumentException("Phone number must be exactly > 11 or 8 < digits.", nameof(phoneCode));
        return new PhoneNumber
        {
            Id = Guid.CreateVersion7(),
            UserGuid=userGuid,
            AddressRegion = addressRegion,
            PhoneCode = phoneCode
        };
        
    }

    public void UpdatePhoneNumber(long addressRegion, string phoneCode)
    {

        if (string.IsNullOrWhiteSpace(phoneCode) || (phoneCode.Length <= 11 && phoneCode.Length >= 8))
            throw new ArgumentException("Phone number must be exactly 11 digits.", nameof(phoneCode));

        AddressRegion = addressRegion;
        PhoneCode = phoneCode;
    }
}