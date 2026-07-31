namespace Identity.Domain.Entities.ClientAggregate;

/// <summary>
/// OAuth 2.0 客户端应用类型，对应 RFC 7591 / RFC 6749 中的 application_type
/// </summary>
public enum ApplicationType
{
    /// <summary>Web 应用（Confidential Client，可安全存储 Secret）</summary>
    Web = 0,

    /// <summary>单页应用 SPA（Public Client，必须使用 PKCE）</summary>
    SPA = 1,

    /// <summary>原生移动应用（Public Client，必须使用 PKCE）</summary>
    Native = 2,

    /// <summary>原生桌面应用（Public Client）</summary>
    Desktop = 3,

    /// <summary>后端服务 / 机器到机器（Confidential Client，通常使用 client_credentials）</summary>
    Service = 4
}
