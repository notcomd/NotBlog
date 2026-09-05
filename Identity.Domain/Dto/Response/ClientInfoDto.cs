namespace Identity.Domain.Dto.Response;

/// <summary>
/// 客户端信息 DTO（不含 ClientSecret — 密钥只在创建时返回一次）
/// </summary>
public sealed record ClientInfoDto(
    Guid NotClientId,
    string ClientId,
    string ApplicationName,
    string? ApplicationDescription,
    string ApplicationType,
    string TokenEndpointAuthMethod,
    string Status,
    HashSet<string> RedirectUris,
    HashSet<string> AllowedGrantTypes,
    HashSet<string> AllowedScopes,
    bool RequirePkce,
    bool RequireConsent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
