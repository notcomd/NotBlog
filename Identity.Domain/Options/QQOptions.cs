namespace Identity.Domain.Options;

/// <summary>
/// QQ 互联 OAuth 2.0 配置（开放平台网站应用）
/// </summary>
public class QQOptions
{
    /// <summary>
    /// QQ 互联应用 ID（client_id）
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// QQ 互联应用密钥（client_secret / AppKey）
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用 QQ 登录
    /// </summary>
    public bool Enabled { get; set; } = false;
}
