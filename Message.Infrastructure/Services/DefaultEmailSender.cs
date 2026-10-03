namespace Message.Infrastructure.Services;

/// <summary>
/// 邮件发送默认实现 — 仅记录日志
/// 后续可替换为接入 SMTP/SendGrid 的实现
/// </summary>
public class DefaultEmailSender : IEmailSender
{
    private readonly ILogger<DefaultEmailSender> _logger;

    /// <summary>初始化 <see cref="DefaultEmailSender"/> 实例。</summary>
    /// <param name="logger">日志记录器。</param>
    public DefaultEmailSender(ILogger<DefaultEmailSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>发送邮件；当前默认实现仅记录日志，不实际投递。</summary>
    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("[预留] 发送Email: To={To}, Subject={Subject}, BodyLength={Length}",
            to, subject, body?.Length ?? 0);
        return Task.CompletedTask;
    }
}
