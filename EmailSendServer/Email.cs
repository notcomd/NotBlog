using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmailSendServer;

public class Email:IEmail
{
    
    private readonly IOptionsSnapshot<EmailAddress> _optionsManager;
    
    public Email(IOptionsSnapshot<EmailAddress> optionsManager)
    {
        _optionsManager = optionsManager;
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="mailPush">配置信息</param>
    public async ValueTask SendEmailValueTask(MimeMessage message,MailPush mailPush)
    {
        message.From.Add(new MailboxAddress(mailPush.FromName,mailPush.FromEmailAddress));
        message.To.AddRange(mailPush.SendEmailAddresses);

        using var mailClient = new SmtpClient
        {
            ServerCertificateValidationCallback = (s,c,h,e)=>true
        };
        mailClient.AuthenticationMechanisms.Remove("XOAUTH2");
        //return ValueTask.CompletedTask;
       await mailClient.ConnectAsync(_optionsManager.Value.AddressHost, _optionsManager.Value.Port,
           SecureSocketOptions.StartTls);
       await mailClient.AuthenticateAsync(_optionsManager.Value.EmailUser, _optionsManager.Value.Password);
       await mailClient.SendAsync(message);
       await mailClient.DisconnectAsync(true);
    }

    public async ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush,SecureSocketOptions secureSocketOptions)
    {
        message.From.Add(new MailboxAddress(mailPush.FromName,mailPush.FromEmailAddress));
        message.To.AddRange(mailPush.SendEmailAddresses);

        using var mailclient = new SmtpClient
        {
            ServerCertificateValidationCallback = (o, c, h, e) => true
        };
        mailclient.AuthenticationMechanisms.Remove("XOAUTH2");
        await mailclient.ConnectAsync(_optionsManager.Value.AddressHost, _optionsManager.Value.Port,
            secureSocketOptions);
        await mailclient.AuthenticateAsync(_optionsManager.Value.EmailUser, _optionsManager.Value.Password);
        await mailclient.SendAsync(message);
        await mailclient.DisconnectAsync(true);
    }
}