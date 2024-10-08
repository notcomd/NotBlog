using MimeKit;

namespace EmailSendServer;

public class MailPush
{
    public MailPush(string title, string fromEmailAddress)
    {
        Title = title;
        this.ToEmailAddress = fromEmailAddress;
    }

    public string Title { get; init; }

    //public string FromName { get; set; }

    public string ToEmailAddress { get; set; }

    public List<MailboxAddress> SendEmailAddresses { get; private set; } = new List<MailboxAddress>();


    public ValueTask AddPushValueTask(MailboxAddress mailboxAddress)
    {
        SendEmailAddresses.Add(mailboxAddress);
        return ValueTask.CompletedTask;
    }
}