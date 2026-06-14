namespace Notcomd.NotEmail;

/// <summary>
/// 已接收邮件模型
/// </summary>
public class ReceivedEmail
{
    /// <summary>
    /// IMAP 唯一 ID
    /// </summary>
    public uint Uid { get; init; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// 发件人地址
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// 收件人地址列表
    /// </summary>
    public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();

    /// <summary>
    /// HTML 正文
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// 纯文本正文
    /// </summary>
    public string? PlainTextBody { get; init; }

    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// 附件列表
    /// </summary>
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = Array.Empty<EmailAttachment>();
}