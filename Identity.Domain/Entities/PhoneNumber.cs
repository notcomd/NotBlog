namespace Identity.Domain.Entities;

public class PhoneNumber
{
    public long AddressRegion { get; set; }

    [StringLength(11, MinimumLength = 11, ErrorMessage = "Phone number is bad!")]
    [Key]
    public string PhoneCode { get; set; } = null!;
}