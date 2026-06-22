namespace Identity.Domain.Entities.UserExternalLoginAggregate;

/// <summary>
/// 登录提供商枚举
/// </summary>
public enum LoginProviderType
{
    /// <summary>Google OAuth 2.0</summary>
    Google,

    /// <summary>Microsoft / Azure AD / Office 365</summary>
    Microsoft,

    /// <summary>GitHub OAuth</summary>
    GitHub,

    /// <summary>微信开放平台 / 公众号 / 小程序</summary>
    WeChat,

    /// <summary>QQ 互联</summary>
    QQ
}