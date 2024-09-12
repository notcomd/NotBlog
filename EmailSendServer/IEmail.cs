using MailKit.Security;
using MimeKit;

namespace EmailSendServer;

public interface IEmail
{
    ValueTask SendEmailValueTask(MimeMessage message,MailPush mailPush );

    ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush, SecureSocketOptions secureSocketOptions);
}