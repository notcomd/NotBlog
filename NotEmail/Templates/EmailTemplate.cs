namespace Notcomd.NotEmail;

/// <summary>
/// 模板存储实体
/// </summary>
public class EmailTemplate
{
    public EmailTemplate(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public EmailTemplate(string name, string subject, string htmlTemplate, string? plainTextTemplate = null)
        : this(name)
    {
        Subject = subject;
        HtmlTemplate = htmlTemplate;
        PlainTextTemplate = plainTextTemplate;
    }

    /// <summary>
    /// 模板名称（唯一标识，如 "WelcomeEmail"）
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 模板标题（支持模板语法）
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML 模板内容（Scriban 语法）
    /// </summary>
    public string HtmlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 纯文本模板内容（可选，作为 HTML 的降级方案）
    /// </summary>
    public string? PlainTextTemplate { get; set; }
}