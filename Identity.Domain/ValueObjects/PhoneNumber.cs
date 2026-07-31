namespace Identity.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    private PhoneNumber()
    {
    } // EF Core needs a parameterless constructor

    private PhoneNumber(long addressRegion, string phoneCode)
    {
        AddressRegion = addressRegion;
        PhoneCode = phoneCode;
    }

    public long AddressRegion { get; private init; }

    public string PhoneCode { get; private init; } = null!;

    public static PhoneNumber CreatePhoneNumber(long addressRegion, string phoneCode)
    {
        if (string.IsNullOrWhiteSpace(phoneCode) || phoneCode.Length != 11)
            throw new ArgumentException("Phone number must be exactly 11 digits.", nameof(phoneCode));
        return new PhoneNumber(addressRegion, phoneCode);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return AddressRegion;
        yield return PhoneCode;
    }

    public override string ToString() => $"+{AddressRegion} {PhoneCode}";
}
