using Microsoft.Extensions.Logging;

namespace Notcomd.NotEmail;

/// <summary>
/// 邮件综合管理器
/// 整合发送、接收、模板渲染、批量操作
/// </summary>
public class EmailManager : IEmailManager
{
    private readonly ILogger<EmailManager>? _logger;
    private readonly IEmailReceiver? _receiver;
    private readonly IEmailSender _sender;
    private readonly TemplateService? _templateService;

    public EmailManager(
        IEmailSender sender,
        IEmailReceiver? receiver = null,
        TemplateService? templateService = null,
        ILogger<EmailManager>? logger = null)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _receiver = receiver;
        _templateService = templateService;
        _logger = logger;
    }

    public Task<SendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        => _sender.SendAsync(message, ct);

    public async Task<SendResult> SendFromTemplateAsync(string templateName, object model, string to,
        string? subject = null, CancellationToken ct = default)
    {
        if (_templateService == null)
            throw new InvalidOperationException("模板服务未注册。请确保已调用 services.AddEmailTemplates()");

        var message = await _templateService.RenderAsync(templateName, model, to, subject);
        return await _sender.SendAsync(message, ct);
    }

    public Task<IReadOnlyList<ReceivedEmail>> GetInboxAsync(int limit = 50, CancellationToken ct = default)
    {
        if (_receiver == null)
            throw new InvalidOperationException("IMAP 接收功能未启用。请在配置中设置 EnableImap=true");
        return _receiver.GetInboxAsync(limit, ct);
    }

    public Task<bool> DeleteAsync(uint uid, CancellationToken ct = default)
    {
        if (_receiver == null)
            throw new InvalidOperationException("IMAP 接收功能未启用。请在配置中设置 EnableImap=true");
        return _receiver.DeleteAsync(uid, ct);
    }

    public Task<SendResult> SendWithOutboxAsync(EmailMessage message, CancellationToken ct = default)
    {
        // Outbox 模式说明:
        // 如果项目中集成了 Evenbus 的 Outbox，应在此处:
        //   1. 将邮件消息写入 OutboxMessages 表
        //   2. 由 OutboxPublisher 后台服务异步发送
        // 
        // 当前简化实现：直接发送（原子性由调用方的事务保证）
        _logger?.LogDebug("[NotEmail] Outbox 模式发送邮件: To={To}, Subject={Subject}",
            message.To, message.Subject);
        return _sender.SendAsync(message, ct);
    }
}