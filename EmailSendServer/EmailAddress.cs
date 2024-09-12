using System.Net.Mail;

namespace EmailSendServer;

public class EmailAddress
{
    public string AddressHost { get; set; } = null!;
    
    public int Port { get; set; }
    
    public bool OptionSsL { get; set; }

    public string EmailUser { get; set; } = null!;
    
    public string Password { get; set; } = null!;
}