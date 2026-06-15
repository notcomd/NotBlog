namespace Notcomd.NotEmail.Core;

/// <summary>
/// 邮件配置选项
/// 
/// 支持两种认证模式:
///   - 密码模式（默认）: Password + SMTP Auth
///   - OAuth 2.0 模式: ClientId/ClientSecret + TokenEndpoint + AccessTokenCallback
/// 
/// OAuth 2.0 适用范围: Google (Gmail)、Microsoft (Outlook/Office 365)、Yahoo Mail
/// 国内服务商（QQ/163/126/Sohu/Aliyun）使用授权码模式，无需 OAuth 2.0。
/// </summary>
public class EmailOptions
{
    // ── 服务器配置 ──
    /// <summary>SMTP 服务器地址</summary>
    public string SmtpHost { get; set; } = "smtp.gmail.com";

    /// <summary>SMTP 端口</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>IMAP 服务器地址</summary>
    public string ImapHost { get; set; } = "imap.gmail.com";

    /// <summary>IMAP 端口</summary>
    public int ImapPort { get; set; } = 993;

    /// <summary>发件人邮箱</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>发件人显示名称</summary>
    public string? FromName { get; set; }

    /// <summary>SMTP 密码 / 应用专用密码（UseOAuth2=false 时使用）</summary>
    public string? Password { get; set; }

    /// <summary>是否使用 SSL</summary>
    public bool UseSsl { get; set; } = true;

    // ── 超时与重试 ──
    /// <summary>发送超时（毫秒）</summary>
    public int SendTimeoutMs { get; set; } = 30000;

    /// <summary>接收超时（毫秒）</summary>
    public int ReceiveTimeoutMs { get; set; } = 30000;

    /// <summary>最大重试次数</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>重试间隔（毫秒）</summary>
    public int RetryIntervalMs { get; set; } = 1000;

    /// <summary>是否启用 IMAP 接收功能</summary>
    public bool EnableImap { get; set; } = false;

    // ── OAuth 2.0 配置 ──
    /// <summary>
    /// 是否启用 OAuth 2.0 认证（true 时使用 AccessTokenCallback 获取 Token）
    /// </summary>
    public bool UseOAuth2 { get; set; }

    /// <summary>
    /// OAuth 2.0 Client ID（在服务商 API 控制台中获取）
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 Client Secret
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Azure AD Tenant ID（仅 Microsoft/Outlook 需要）
    /// "common" = 多租户, "organizations" = 组织账号, "consumers" = 个人账号
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// OAuth 2.0 授权端点 URL
    /// </summary>
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// OAuth 2.0 Token 端点 URL
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// OAuth 2.0 权限范围列表
    /// 默认: ["email", "profile", ...]
    /// SMTP 需要: ... (具体见服务商文档)
    /// </summary>
    public List<string> OAuthScopes { get; set; } = new();

    /// <summary>
    /// OAuth 2.0 令牌获取回调（异步）
    /// 
    /// 用于获取/刷新 Access Token。实现应包含:
    ///   1. 使用 ClientId/ClientSecret 请求 TokenEndpoint
    ///   2. 缓存 Token 并检查过期
    ///   3. 返回有效的 AccessToken 字符串
    /// 
    /// 示例:
    ///   AccessTokenCallback = async ct => {
    ///       var token = await tokenService.GetOrRefreshTokenAsync();
    ///       return token.AccessToken;
    ///   };
    /// </summary>
    public Func<CancellationToken, Task<string>>? AccessTokenCallback { get; set; }

    // ── 辅助方法 ──
    /// <summary>
    /// 检查 OAuth 2.0 配置是否完整
    /// </summary>
    public bool IsOAuth2Configured =>
        UseOAuth2 && AccessTokenCallback != null;

    /// <summary>
    /// 检查密码认证配置是否有效
    /// </summary>
    public bool IsPasswordConfigured =>
        !string.IsNullOrEmpty(Password);
}