namespace Identity.Domain.Entities;

public class PhoneNumber : Entity
{

    protected PhoneNumber() {} // EF Core needs a parameterless constructor

    public long AddressRegion { get; set; }

    public string PhoneCode { get; set; } = null!;

    public Guid UserGuid { get; private set; } //外键

    public static PhoneNumber CreatePhoneNumber(long addressRegion, string phoneCode)
    {
        if (string.IsNullOrWhiteSpace(phoneCode) || phoneCode.Length != 11)
            throw new ArgumentException("Phone number must be exactly 11 digits.", nameof(phoneCode));
        var phoneNumber = new PhoneNumber
        {
            AddressRegion = addressRegion,
            PhoneCode = phoneCode
        };
        return phoneNumber;
    }

    public void UpdatePhoneNumber(long addressRegion, string phoneCode)
    {
        if (string.IsNullOrWhiteSpace(phoneCode) || phoneCode.Length != 11)
            throw new ArgumentException("Phone number must be exactly 11 digits.", nameof(phoneCode));
        AddressRegion = addressRegion;
        PhoneCode = phoneCode;
    }
}