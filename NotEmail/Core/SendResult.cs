namespace Notcomd.NotEmail.Core;

/// <summary>
/// 邮件发送结果
/// </summary>
public class SendResult
{
    /// <summary>
    /// 是否发送成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 发送失败时的错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// SMTP 服务器返回的消息 ID
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// 发送耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; init; }

    public static SendResult Ok(string? messageId = null) =>
        new() { Success = true, MessageId = messageId };

    public static SendResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}