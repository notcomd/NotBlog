namespace EmailSendServer;

public class EmailAddress
{
    public string SmtpHost { get; set; } = null!;
    public int Port { get; set; }

    public bool OptionSsL { get; set; }

    public string FromEmail { get; set; } = null!;

    public string SmtpPassword { get; set; } = null!;
}