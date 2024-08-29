namespace Identity.Domain.Entities;

public class PhoneNumber
{
    public long AddressRegion { get; set; }
    public string PhoneCode { get; set; } = null!;
}