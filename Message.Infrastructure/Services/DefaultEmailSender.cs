using Message.Domain.IServices;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services;

/// <summary>
/// 邮件发送默认实现 — 仅记录日志
/// 后续可替换为接入 SMTP/SendGrid 的实现
/// </summary>
public class DefaultEmailSender : IEmailSender
{
    private readonly ILogger<DefaultEmailSender> _logger;

    public DefaultEmailSender(ILogger<DefaultEmailSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("[预留] 发送Email: To={To}, Subject={Subject}, BodyLength={Length}",
            to, subject, body?.Length ?? 0);
        return Task.CompletedTask;
    }
}
