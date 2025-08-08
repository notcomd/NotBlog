using Org.BouncyCastle.Asn1.Cms;

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
        ArgumentNullException.ThrowIfNull(toEmail, nameof(toEmail));
        ArgumentNullException.ThrowIfNull(code, nameof(code));

        var mailpush = new MailPush("验证玛", toEmail,PushEmailTemplate(code));

        var message = new MimeMessage
        {
            Subject = "hello",
            Body = new BodyBuilder
            {
                HtmlBody =mailpush.BodyEmail
                    
            }.ToMessageBody()
        };

        try
        {
            await _email.SendEmailValueTask(message, mailpush, SecureSocketOptions.SslOnConnect);
            _logger.LogInformation($"[（*＾-＾*）{0}] 邮件发送成功，接收人：{toEmail}",DateTimeOffset.UtcNow);
        }
        catch (Exception e)
        {
            _logger.LogError($"[(≧ ﹏ ≦){DateTimeOffset.UtcNow}] 邮件发送失败，接收人：{toEmail}，错误信息：{e.Message}");
            throw;
        }

        
    }

    private string PushEmailTemplate(string code)
    {
        return $@"
            <h1>欢迎使用我们的服务！</h1>
            <p>您的验证码是: <strong>{code}</strong></p>
            <p>请在10分钟内使用此验证码。</p>
            <p>如果您没有请求此验证码，请忽略此邮件。</p>
            <br/>
            <p>谢谢！</p>
        ";
    }

}