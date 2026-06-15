namespace Notcomd.NotEmail.Core;

/// <summary>
/// 邮件发送接口
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// 发送单封邮件
    /// </summary>
    Task<SendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发送邮件
    /// </summary>
    Task<IReadOnlyList<SendResult>> SendBatchAsync(IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default);
}