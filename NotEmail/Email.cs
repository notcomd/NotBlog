using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace EmailSendServer;

public class Email : IEmail
{
    private readonly ILogger<Email> _logger;

    private readonly IOptionsSnapshot<EmailOptions> _optionsManager;

    public Email(IOptionsSnapshot<EmailOptions> optionsManager, ILogger<Email> logger)
    {
        _optionsManager = optionsManager ?? throw new ArgumentNullException($"{optionsManager}不能为空", nameof(optionsManager));
        _logger = logger ?? throw new ArgumentNullException($"{logger}不能为空", nameof(logger));
    }


    public async ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush)
    {
        await SendEmailInternalValueTask(message, mailPush, SecureSocketOptions.StartTls);
    }


    /// <summary>
    /// 使用安全的连接发送邮件
    /// </summary>
    /// <param name="message"></param>
    /// <param name="mailPush">配置信息</param>
    public async ValueTask SendEmailValueTask(MimeMessage message, MailPush mailPush,SecureSocketOptions secureSocketOptions)
    {
       await SendEmailInternalValueTask(message, mailPush, secureSocketOptions);
    }

    private async ValueTask SendEmailInternalValueTask(MimeMessage message, MailPush mailPush,
        SecureSocketOptions secureSocketOptions)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        ArgumentNullException.ThrowIfNull(mailPush, nameof(mailPush));
        ArgumentNullException.ThrowIfNull(_optionsManager.Value, nameof(_optionsManager.Value));

        try
        {
           ConfigMessage(message, mailPush);
            using var smtpClient = new SmtpClient();
            
            await ConnectionAndSendAsync(smtpClient, message, secureSocketOptions);
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow}]邮件发送成功，接收人：{string.Join(",", mailPush.ToEmailList.Select(x => x.Address))}，主题：{message.Subject}");

        }
        catch (SmtpCommandException e)
        {
            Console.WriteLine($"Error>{e.ErrorCode}-{e.Message}");
            _logger.LogError(e.Message);
        }

    }

    /// <summary>
    /// 配置邮件信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="mailPush"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private void ConfigMessage(MimeMessage message, MailPush mailPush)
    {
        try
        {
            message.From.Add(new MailboxAddress(_optionsManager.Value.FromEmail, _optionsManager.Value.FromEmail));
            if (mailPush.ToEmailList.Any() == true)
            {
                message.To.AddRange(mailPush.ToEmailList);
            }
            else
            {
                if (string.IsNullOrEmpty(mailPush.ToEmailAddress) == false)
                {
                    message.To.Add(new MailboxAddress(mailPush.ToEmailAddress, mailPush.ToEmailAddress));
                }
                else
                {
                    throw new ArgumentNullException(nameof(mailPush.ToEmailAddress), "邮件接收地址不能为空");
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[(≧ ﹏ ≦)]配置邮件信息失败");
            throw;
        }
    }

    /// <summary>
    ///  设置连接并发送邮件
    /// </summary>
    /// <param name="smtpClient"></param>
    /// <param name="mimeMessage"></param>
    /// <param name="secureSocketOptions"></param>
    /// <returns></returns>
    private async ValueTask ConnectionAndSendAsync(SmtpClient smtpClient, MimeMessage mimeMessage, SecureSocketOptions secureSocketOptions)
    {
        try
        {
            smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
            await smtpClient.ConnectAsync(_optionsManager.Value.SmtpHost, _optionsManager.Value.Port, secureSocketOptions);
            await smtpClient.AuthenticateAsync(_optionsManager.Value.FromEmail, _optionsManager.Value.SmtpPassword);
            await smtpClient.SendAsync(mimeMessage);
            await smtpClient.DisconnectAsync(true);
        }
        catch (SmtpCommandException e)
        {
            Console.WriteLine(e.Message);
            _logger.LogError(e.Message);
        }
    }
}