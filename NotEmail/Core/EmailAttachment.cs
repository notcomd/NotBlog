namespace Notcomd.NotEmail;

/// <summary>
/// 邮件附件模型
/// </summary>
public class EmailAttachment
{
    public EmailAttachment(string fileName, byte[] data, string? mimeType = null)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        MimeType = mimeType;
    }

    /// <summary>
    /// 文件名（如 "report.pdf"）
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 文件字节数据
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// MIME 类型（如 "application/pdf"）
    /// </summary>
    public string? MimeType { get; set; }
}