using MimeKit;

namespace EmailSendServer;

public class MailPush
{

    public MailPush(string titleEmail, string toEmailAddress)
    {
        TitleEmail = titleEmail;
        ToEmailAddress = toEmailAddress;
    }

    /// <summary>
    ///     邮件主题
    /// </summary>
    public string TitleEmail { get; private set; }
    /// <summary>
    ///     邮件接收地址
    /// </summary>
    public string ToEmailAddress { get; set; }

    public List<MailboxAddress> ToEmailList { get; } = new();

    public ValueTask AddPushValueTask(MailboxAddress mailboxAddress)
    {
        ToEmailList.Add(mailboxAddress);
        return ValueTask.CompletedTask;
    }
}