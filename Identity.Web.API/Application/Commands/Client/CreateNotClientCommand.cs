using Identity.Web.API.Application.Commands;

namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 创建 OAuth 客户端（RFC 7591 Dynamic Client Registration）
/// clientId 不传时由服务端生成；clientSecret 始终由服务端生成并仅在此次响应返回一次
/// </summary>
public record CreateNotClientCommand(
    string ApplicationName,
    HashSet<string> RedirectUris,
    HashSet<string> AllowedGrantTypes,
    HashSet<string> AllowedScopes,
    ApplicationType ApplicationType,
    string TokenEndpointAuthMethod,
    string? ClientId = null,
    string? ApplicationDescription = null,
    string? ApplicationIcon = null,
    string? HomepageUri = null,
    string? PrivacyPolicyUri = null,
    string? TermsOfServiceUri = null,
    string? ContactEmail = null,
    HashSet<string>? PostLogoutRedirectUris = null,
    HashSet<string>? AllowedCorsOrigins = null,
    bool RequirePkce = true,
    bool RequireConsent = false
) : IRequest<CreateNotClientResult>, ILoggableCommand
{
    public string IdProperty => nameof(ApplicationName);
    public string IdValue => ApplicationName;
}
