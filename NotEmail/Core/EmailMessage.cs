namespace Notcomd.NotEmail;

/// <summary>
/// 邮件消息模型（发送用）
/// </summary>
public class EmailMessage
{
    public EmailMessage(string to, string subject, string body, bool isHtml = true)
    {
        To = to ?? throw new ArgumentNullException(nameof(to));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        if (isHtml)
            HtmlBody = body;
        else
            PlainTextBody = body;
    }

    public EmailMessage(string to)
    {
        To = to ?? throw new ArgumentNullException(nameof(to));
    }

    /// <summary>
    /// 收件人地址（必填）
    /// </summary>
    public string To { get; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML 正文
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// 纯文本正文（HTML 不支持时的替补）
    /// </summary>
    public string? PlainTextBody { get; set; }

    /// <summary>
    /// 发件人显示名称
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// 抄送地址列表
    /// </summary>
    public List<string> Cc { get; set; } = new();

    /// <summary>
    /// 密送地址列表
    /// </summary>
    public List<string> Bcc { get; set; } = new();

    /// <summary>
    /// 附件列表
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// 邮件优先级
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
}

/// <summary>
/// 邮件优先级
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}