namespace Notcomd.NotEmail.Core;

/// <summary>
/// 邮件管理器综合接口（发送 + 接收 + 管理）
/// </summary>
public interface IEmailManager
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    Task<SendResult> SendAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>
    /// 使用模板发送邮件
    /// </summary>
    Task<SendResult> SendFromTemplateAsync(string templateName, object model, string to,
        string? subject = null, CancellationToken ct = default);

    /// <summary>
    /// 获取收件箱
    /// </summary>
    Task<IReadOnlyList<ReceivedEmail>> GetInboxAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// 删除邮件
    /// </summary>
    Task<bool> DeleteAsync(uint uid, CancellationToken ct = default);

    /// <summary>
    /// 发送邮件（Outbox 模式：先写数据库再发送）
    /// </summary>
    Task<SendResult> SendWithOutboxAsync(EmailMessage message, CancellationToken ct = default);
}