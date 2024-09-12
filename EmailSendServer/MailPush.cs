using MimeKit;

namespace EmailSendServer;

public class MailPush
{
    public MailPush(string title, string fromName, string fromEmailAddress)
    {
        Title = title;
        FromName = fromName;
        this.FromEmailAddress = fromEmailAddress;
    }

    public string Title { get; set; } = null!;
    
    public string FromName { get; set; }
    
    public string FromEmailAddress { get; set; }

    public List<MailboxAddress> SendEmailAddresses { get; set; } = new List<MailboxAddress>();
    
    

    public ValueTask AddPushValueTask(MailboxAddress mailboxAddress)
    {
        SendEmailAddresses.Add(mailboxAddress);
        return ValueTask.CompletedTask;
    }

}