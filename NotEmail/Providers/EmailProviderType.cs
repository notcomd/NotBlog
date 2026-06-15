namespace Notcomd.NotEmail.Providers;

/// <summary>
/// 支持的邮件服务商类型
/// </summary>
public enum EmailProviderType
{
    /// <summary>Gmail / Google Workspace</summary>
    Google,

    /// <summary>Outlook / Hotmail / Office 365</summary>
    Outlook,

    /// <summary>QQ 邮箱</summary>
    QQ,

    /// <summary>网易 163 邮箱</summary>
    NetEase,

    /// <summary>网易 126 邮箱</summary>
    NetEase126,

    /// <summary>阿里云企业邮箱</summary>
    Aliyun,

    /// <summary>Yahoo Mail</summary>
    Yahoo,

    /// <summary>搜狐邮箱</summary>
    Sohu,

    /// <summary>自定义配置</summary>
    Custom
}