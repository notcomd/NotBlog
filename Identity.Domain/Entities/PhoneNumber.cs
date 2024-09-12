using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;

namespace Identity.Domain.Entities;

public class PhoneNumber
{
    public long AddressRegion { get; set; }
    [StringLength(maximumLength:11,MinimumLength = 11,ErrorMessage = "Phone number is bad!")]
    public string PhoneCode { get; set; } = null!;
}