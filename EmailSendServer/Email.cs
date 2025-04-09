using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailSendServer;

public class Email : IEmail
{
    private readonly IOptionsSnapshot<EmailAddress> _optionsManager;

    public Email(IOptionsSnapshot<EmailAddress> optionsManager)
    {
        _optionsManager = optionsManager;
    }


    /// <summary>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="mailPush">配置信息</param>
    public async ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush)
    {
        try
        {
            message.From.Add(new MailboxAddress(_optionsManager.Value.FromEmail, _optionsManager.Value.FromEmail));
            message.To.AddRange(mailPush.ToEmailList);

            using (var mailClient = new SmtpClient())
            {
                mailClient.AuthenticationMechanisms.Remove("XOAUTH2");
                //return ValueTask.CompletedTask;
                await mailClient.ConnectAsync(_optionsManager.Value.SmtpHost, _optionsManager.Value.Port,
                    SecureSocketOptions.StartTls);
                await mailClient.AuthenticateAsync(_optionsManager.Value.SmtpHost, _optionsManager.Value.SmtpPassword);
                await mailClient.SendAsync(message);
                await mailClient.DisconnectAsync(true);
            }

        }
        catch (SmtpCommandException e)
        {
            Console.WriteLine(e.Message);
        }

    }

    public async ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush,
        SecureSocketOptions secureSocketOptions)
    {
        try
        {
            if (_optionsManager.Value is null)
            {
                throw new ArgumentNullException(nameof(_optionsManager.Value.FromEmail));
            }
            message.From.Add(new MailboxAddress("", _optionsManager.Value.FromEmail));
            message.To.Add(new MailboxAddress("", mailPush.ToEmailAddress));

            using (var mailclient = new SmtpClient())
            {
                await mailclient.ConnectAsync(_optionsManager.Value.SmtpHost, _optionsManager.Value.Port,
                    secureSocketOptions);
                await mailclient.AuthenticateAsync(_optionsManager.Value.FromEmail,
                    _optionsManager.Value.SmtpPassword);
                await mailclient.SendAsync(message);
                await mailclient.DisconnectAsync(true);
            }
        }
        catch (SmtpCommandException e)
        {
            Console.WriteLine($"Error>{e.ErrorCode}-{e.Message}");
        }

    }
}