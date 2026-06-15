using Notcomd.NotEmail.Core;

namespace Notcomd.NotEmail.Providers;

/// <summary>
/// 主流邮件服务商预置配置
/// 
/// 一键配置常用邮箱，无需手动填 SMTP/IMAP 信息。
/// 
/// 用法:
///   var options = EmailProviderConfig.Google("your@gmail.com", "app-password");
///   var options = EmailProviderConfig.Outlook("your@outlook.com", "password");
///   var options = EmailProviderConfig.QQ("your@qq.com", "授权码");
///   var options = EmailProviderConfig.NetEase163("your@163.com", "授权码");
///   var options = EmailProviderConfig.Yahoo("your@yahoo.com", "app-password");
///   var options = EmailProviderConfig.Sohu("your@sohu.com", "授权码");
/// 
/// OAuth 2.0 用法（推荐用于 Google / Outlook / Yahoo）:
///   var options = EmailProviderConfig.GoogleOAuth2(clientId, clientSecret, tokenCallback);
///   var options = EmailProviderConfig.OutlookOAuth2(clientId, clientSecret, tokenCallback, tenantId);
///   var options = EmailProviderConfig.YahooOAuth2(clientId, clientSecret, tokenCallback);
/// </summary>
public static class EmailProviderConfig
{
    /// <summary>
    /// Gmail 配置（需使用"应用专用密码"）
    /// 获取地址: https://myaccount.google.com/apppasswords
    /// </summary>
    public static EmailOptions Google(string email, string appPassword)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            ImapHost = "imap.gmail.com",
            ImapPort = 993,
            FromEmail = email,
            Password = appPassword,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// Outlook / Hotmail / Office 365 配置
    /// </summary>
    public static EmailOptions Outlook(string email, string password)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp-mail.outlook.com",
            SmtpPort = 587,
            ImapHost = "outlook.office365.com",
            ImapPort = 993,
            FromEmail = email,
            Password = password,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// QQ 邮箱配置（需开启 SMTP/IMAP 并使用"授权码"）
    /// 设置路径: QQ邮箱 → 设置 → 账户 → POP3/IMAP/SMTP 服务
    /// </summary>
    public static EmailOptions QQ(string email, string authCode)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.qq.com",
            SmtpPort = 587,
            ImapHost = "imap.qq.com",
            ImapPort = 993,
            FromEmail = email,
            Password = authCode,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// 网易 163 邮箱配置（需开启 SMTP/IMAP 并使用"授权码"）
    /// </summary>
    public static EmailOptions NetEase163(string email, string authCode)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.163.com",
            SmtpPort = 465,
            ImapHost = "imap.163.com",
            ImapPort = 993,
            FromEmail = email,
            Password = authCode,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// 网易 126 邮箱配置
    /// </summary>
    public static EmailOptions NetEase126(string email, string authCode)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.126.com",
            SmtpPort = 465,
            ImapHost = "imap.126.com",
            ImapPort = 993,
            FromEmail = email,
            Password = authCode,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// 阿里云企业邮箱配置
    /// </summary>
    public static EmailOptions Aliyun(string email, string password)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.mxhichina.com",
            SmtpPort = 465,
            ImapHost = "imap.mxhichina.com",
            ImapPort = 993,
            FromEmail = email,
            Password = password,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// Yahoo Mail 配置（需使用"应用专用密码"）
    /// 获取地址: https://login.yahoo.com/account/security → App Passwords
    /// </summary>
    public static EmailOptions Yahoo(string email, string appPassword)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.mail.yahoo.com",
            SmtpPort = 587,
            ImapHost = "imap.mail.yahoo.com",
            ImapPort = 993,
            FromEmail = email,
            Password = appPassword,
            UseSsl = true,
            EnableImap = true
        };
    }

    /// <summary>
    /// 搜狐邮箱配置（需开启 SMTP/IMAP 并使用"授权码"）
    /// 设置路径: 搜狐邮箱 → 设置 → 账户 → POP3/IMAP/SMTP 服务
    /// </summary>
    public static EmailOptions Sohu(string email, string authCode)
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.sohu.com",
            SmtpPort = 465,
            ImapHost = "imap.sohu.com",
            ImapPort = 993,
            FromEmail = email,
            Password = authCode,
            UseSsl = true,
            EnableImap = true
        };
    }

    // ──────────────────────────────────────
    //  OAuth 2.0 预置配置（推荐）
    // ──────────────────────────────────────

    /// <summary>
    /// Gmail OAuth 2.0 配置（推荐替代应用专用密码）
    /// 
    /// 使用步骤:
    ///   1. Google Cloud Console → 创建 OAuth 2.0 客户端 ID
    ///   2. 获取 ClientId 和 ClientSecret
    ///   3. 在 AccessTokenCallback 中实现 Token 获取/刷新逻辑
    /// 
    /// TokenCallback 示例（使用 Google.Apis.Auth）:
    ///   AccessTokenCallback = async ct => {
    ///       var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(...);
    ///       return await credential.GetAccessTokenForRequestAsync();
    ///   };
    /// </summary>
    /// <param name="clientId">Google OAuth 2.0 Client ID</param>
    /// <param name="clientSecret">Google OAuth 2.0 Client Secret</param>
    /// <param name="tokenCallback">Token 获取/刷新回调（返回 access_token）</param>
    /// <param name="email">发件人邮箱</param>
    public static EmailOptions GoogleOAuth2(
        string clientId, string clientSecret,
        Func<CancellationToken, Task<string>> tokenCallback,
        string email = "")
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            ImapHost = "imap.gmail.com",
            ImapPort = 993,
            FromEmail = email,
            UseSsl = true,
            EnableImap = true,
            UseOAuth2 = true,
            ClientId = clientId,
            ClientSecret = clientSecret,
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            OAuthScopes = new List<string>
            {
                "https://mail.google.com/" // Gmail 全线访问（SMTP + IMAP）
            },
            AccessTokenCallback = tokenCallback
        };
    }

    /// <summary>
    /// Outlook / Office 365 OAuth 2.0 配置（Microsoft Identity Platform）
    /// 
    /// 使用步骤:
    ///   1. Azure Portal → App registrations → 注册应用
    ///   2. 获取 Application (client) ID 和 Directory (tenant) ID
    ///   3. 创建 Client Secret
    ///   4. API Permissions 中添加 SMTP.Send 和 IMAP.AccessAsUser.All
    /// 
    /// TokenCallback 示例（使用 MSAL）:
    ///   AccessTokenCallback = async ct => {
    ///       var app = ConfidentialClientApplicationBuilder.Create(clientId)
    ///           .WithClientSecret(clientSecret)
    ///           .WithTenantId(tenantId)
    ///           .Build();
    ///       var result = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct);
    ///       return result.AccessToken;
    ///   };
    /// </summary>
    /// <param name="clientId">Azure AD Application (client) ID</param>
    /// <param name="clientSecret">Client Secret</param>
    /// <param name="tokenCallback">Token 获取/刷新回调</param>
    /// <param name="tenantId">Tenant ID（默认 "common"）</param>
    /// <param name="email">发件人邮箱</param>
    public static EmailOptions OutlookOAuth2(
        string clientId, string clientSecret,
        Func<CancellationToken, Task<string>> tokenCallback,
        string tenantId = "common", string email = "")
    {
        return new EmailOptions
        {
            SmtpHost = "smtp-mail.outlook.com",
            SmtpPort = 587,
            ImapHost = "outlook.office365.com",
            ImapPort = 993,
            FromEmail = email,
            UseSsl = true,
            EnableImap = true,
            UseOAuth2 = true,
            ClientId = clientId,
            ClientSecret = clientSecret,
            TenantId = tenantId,
            AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            OAuthScopes = new List<string>
            {
                "https://outlook.office.com/SMTP.Send",
                "https://outlook.office.com/IMAP.AccessAsUser.All",
                "offline_access"
            },
            AccessTokenCallback = tokenCallback
        };
    }

    /// <summary>
    /// Yahoo Mail OAuth 2.0 配置
    /// 
    /// 使用步骤:
    ///   1. Yahoo Developer Network → 创建应用
    ///   2. 获取 Client ID 和 Client Secret
    ///   3. 添加 SMTP/IMAP 权限范围
    /// </summary>
    /// <param name="clientId">Yahoo OAuth 2.0 Client ID</param>
    /// <param name="clientSecret">Yahoo Client Secret</param>
    /// <param name="tokenCallback">Token 获取/刷新回调</param>
    /// <param name="email">发件人邮箱</param>
    public static EmailOptions YahooOAuth2(
        string clientId, string clientSecret,
        Func<CancellationToken, Task<string>> tokenCallback,
        string email = "")
    {
        return new EmailOptions
        {
            SmtpHost = "smtp.mail.yahoo.com",
            SmtpPort = 587,
            ImapHost = "imap.mail.yahoo.com",
            ImapPort = 993,
            FromEmail = email,
            UseSsl = true,
            EnableImap = true,
            UseOAuth2 = true,
            ClientId = clientId,
            ClientSecret = clientSecret,
            AuthorizationEndpoint = "https://api.login.yahoo.com/oauth2/request_auth",
            TokenEndpoint = "https://api.login.yahoo.com/oauth2/get_token",
            OAuthScopes = new List<string>
            {
                "mail-w" // Yahoo Mail 读写权限
            },
            AccessTokenCallback = tokenCallback
        };
    }

    /// <summary>
    /// 根据 ProviderType 自动生成配置</summary>
    public static EmailOptions FromProvider(EmailProviderType provider, string email, string password) =>
        provider switch
        {
            EmailProviderType.Google => Google(email, password),
            EmailProviderType.Outlook => Outlook(email, password),
            EmailProviderType.QQ => QQ(email, password),
            EmailProviderType.NetEase => NetEase163(email, password),
            EmailProviderType.NetEase126 => NetEase126(email, password),
            EmailProviderType.Aliyun => Aliyun(email, password),
            EmailProviderType.Yahoo => Yahoo(email, password),
            EmailProviderType.Sohu => Sohu(email, password),
            _ => throw new ArgumentException(
                $"不支持的服务商: {provider}。Custom 类型请手动构建 EmailOptions。", nameof(provider))
        };
}