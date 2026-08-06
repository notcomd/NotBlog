using Identity.Web.API.Application.Commands;

namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 更新 OAuth 客户端（字段为 null 表示不修改；集合字段传入即整体替换）
/// </summary>
public record UpdateNotClientCommand(
    Guid NotClientId,
    string? ApplicationName = null,
    string? ApplicationDescription = null,
    string? ApplicationIcon = null,
    string? HomepageUri = null,
    string? PrivacyPolicyUri = null,
    string? TermsOfServiceUri = null,
    string? ContactEmail = null,
    HashSet<string>? RedirectUris = null,
    HashSet<string>? AllowedGrantTypes = null,
    HashSet<string>? AllowedScopes = null,
    HashSet<string>? AllowedCorsOrigins = null
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(NotClientId);
    public string IdValue => NotClientId.ToString();
}
