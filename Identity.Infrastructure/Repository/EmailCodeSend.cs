using EmailSendServer;
using Identity.Domain.IRepository;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Identity.Infrastructure.Repository;

public class EmailCodeSend : IEmailCodeSend
{
    private readonly IEmail _email;
    private readonly ILogger<IEmail> _logger;

    public EmailCodeSend(IEmail email, ILogger<IEmail> logger)
    {
        _email = email;
        _logger = logger;
    }

    public async ValueTask SendEmailCodeAsync(string toEmail, string code)
    {
        var mailpush = new MailPush("验证玛", toEmail);

        var message = new MimeMessage
        {
            Subject = "hello",
            Body = new BodyBuilder
            {
                HtmlBody =
                    $"<dir style=\"background-color: deepskyblue; width: auto; height: 60px;\">\n    <span style=\"text-align: left;\"><h1>Notcomd Studio</h1></span>\n</dir>\n<dir style=\" width: auto; height: max-content;\">\n    <span style=\"text-align: center;\"><h1>验证码</h1></span>\n    <span style=\"text-align:center;\"><h2>{code}</h2></span>\n</dir>"
            }.ToMessageBody()
        };
        await _email.SendEmailValueTask(message, mailpush, SecureSocketOptions.StartTls);
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
    }
}