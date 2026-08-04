namespace Identity.Domain.Entities.ClientAggregate;

/// <summary>
/// OAuth 2.0 客户端注册实体（聚合根）
/// 
/// 对应 RFC 7591 (OAuth 2.0 Dynamic Client Registration Protocol) 中的客户端元数据，
/// 同时兼容 RFC 6749 (OAuth 2.0 Authorization Framework) 的客户端定义。
/// 
/// 用于外部服务在本认证中心登记 OAuth 客户端时存储的核心数据模型。
/// </summary>
public class NotClient : Entity<int>, IAggregateRoot
{
    // ── 构造函数 ──

    protected NotClient()
    {
    }

    public NotClient(
        string clientId,
        string applicationName,
        string clientSecret,
        HashSet<string> redirectUris,
        HashSet<string> allowedGrantTypes,
        HashSet<string> allowedScopes,
        ApplicationType applicationType,
        string tokenEndpointAuthMethod,
        string? applicationDescription = null,
        string? applicationIcon = null,
        string? homepageUri = null,
        string? privacyPolicyUri = null,
        string? termsOfServiceUri = null,
        string? contactEmail = null,
        HashSet<string>? postLogoutRedirectUris = null,
        HashSet<string>? allowedCorsOrigins = null,
        bool requirePkce = true,
        bool requireConsent = false)
    {
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        RedirectUris = redirectUris ?? throw new ArgumentNullException(nameof(redirectUris));
        AllowedGrantTypes = allowedGrantTypes ?? throw new ArgumentNullException(nameof(allowedGrantTypes));
        AllowedScopes = allowedScopes ?? throw new ArgumentNullException(nameof(allowedScopes));
        TokenEndpointAuthMethod = tokenEndpointAuthMethod ?? throw new ArgumentNullException(nameof(tokenEndpointAuthMethod));

        if (redirectUris.Count == 0)
            throw new ArgumentException("至少需要一个 Redirect URI", nameof(redirectUris));

        if (allowedGrantTypes.Count == 0)
            throw new ArgumentException("至少需要一种 Grant Type", nameof(allowedGrantTypes));

        ApplicationDescription = applicationDescription;
        ApplicationIcon = applicationIcon;
        HomepageUri = homepageUri;
        PrivacyPolicyUri = privacyPolicyUri;
        TermsOfServiceUri = termsOfServiceUri;
        ContactEmail = contactEmail;
        PostLogoutRedirectUris = postLogoutRedirectUris ?? [];
        AllowedCorsOrigins = allowedCorsOrigins;
        ApplicationType = applicationType;
        RequirePkce = requirePkce;
        RequireConsent = requireConsent;

        Status = ClientStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── 标识 ──

    /// <summary>内部数据库主键</summary>
    public Guid NotClientId { get; init; } = Guid.CreateVersion7();

    /// <summary>OAuth 2.0 client_id（对外暴露的客户端标识符）</summary>
    public string ClientId { get; private set; } = null!;

    // ── 应用基本信息 ──

    /// <summary>应用名称（对应 RFC 7591 §2 中的 client_name）</summary>
    public string ApplicationName { get; private set; } = null!;

    /// <summary>应用描述（对应 client_description）</summary>
    public string? ApplicationDescription { get; private set; }

    /// <summary>应用图标 URL（对应 logo_uri）</summary>
    public string? ApplicationIcon { get; private set; }

    /// <summary>应用主页 URL（对应 client_uri）</summary>
    public string? HomepageUri { get; private set; }

    /// <summary>隐私政策 URL（对应 policy_uri）</summary>
    public string? PrivacyPolicyUri { get; private set; }

    /// <summary>服务条款 URL（对应 tos_uri）</summary>
    public string? TermsOfServiceUri { get; private set; }

    // ── 联系方式 ──

    /// <summary>联系邮箱（对应 contacts）</summary>
    public string? ContactEmail { get; private set; }

    // ── OAuth 2.0 核心字段 ──

    /// <summary>客户端密钥（加密存储，对应 client_secret）</summary>
    public string ClientSecret { get; private set; } = null!;

    /// <summary>授权回调地址（对应 redirect_uris，至少一个）</summary>
    public HashSet<string> RedirectUris { get; private set; } = [];

    /// <summary>登出后回调地址（对应 post_logout_redirect_uris，OpenID Connect）</summary>
    public HashSet<string> PostLogoutRedirectUris { get; private set; } = [];

    /// <summary>允许的授权范围（对应 scope）</summary>
    public HashSet<string> AllowedScopes { get; private set; } = [];

    /// <summary>允许的授权模式（对应 grant_types）</summary>
    public HashSet<string> AllowedGrantTypes { get; private set; } = [];

    // ── 安全与类型 ──

    /// <summary>应用类型（Web/SPA/Native/Desktop/Service）</summary>
    public ApplicationType ApplicationType { get; private set; }

    /// <summary>Token 端点认证方式（client_secret_basic / client_secret_post / private_key_jwt / none）</summary>
    public string TokenEndpointAuthMethod { get; private set; } = null!;

    /// <summary>是否强制 PKCE（Public Client 必须启用）</summary>
    public bool RequirePkce { get; private set; }

    /// <summary>是否要求用户同意授权</summary>
    public bool RequireConsent { get; private set; }

    /// <summary>允许的 CORS 来源（Web/SPA 客户端使用）</summary>
    public HashSet<string>? AllowedCorsOrigins { get; private set; }

    // ── 状态与审计 ──

    /// <summary>客户端状态（Active / Disabled / Revoked）</summary>
    public ClientStatus Status { get; private set; }

    /// <summary>注册时间</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>最后更新时间</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── 领域行为 ──

    /// <summary>更新客户端密钥（密钥轮换）</summary>
    public void RotateSecret(string newSecret)
    {
        if (string.IsNullOrWhiteSpace(newSecret))
            throw new ArgumentException("密钥不能为空", nameof(newSecret));
        ClientSecret = newSecret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>更新应用基本信息</summary>
    public void UpdateApplicationInfo(
        string? applicationName = null,
        string? applicationDescription = null,
        string? applicationIcon = null,
        string? homepageUri = null,
        string? privacyPolicyUri = null,
        string? termsOfServiceUri = null,
        string? contactEmail = null)
    {
        if (applicationName is not null)
            ApplicationName = applicationName;
        if (applicationDescription is not null)
            ApplicationDescription = applicationDescription;
        if (applicationIcon is not null)
            ApplicationIcon = applicationIcon;
        if (homepageUri is not null)
            HomepageUri = homepageUri;
        if (privacyPolicyUri is not null)
            PrivacyPolicyUri = privacyPolicyUri;
        if (termsOfServiceUri is not null)
            TermsOfServiceUri = termsOfServiceUri;
        if (contactEmail is not null)
            ContactEmail = contactEmail;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>更新允许的授权类型</summary>
    public void UpdateGrantTypes(HashSet<string> grantTypes)
    {
        if (grantTypes is null || grantTypes.Count == 0)
            throw new ArgumentException("至少需要一种 Grant Type", nameof(grantTypes));
        AllowedGrantTypes = grantTypes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>更新允许的授权范围</summary>
    public void UpdateScopes(HashSet<string> scopes)
    {
        if (scopes is null || scopes.Count == 0)
            throw new ArgumentException("至少需要一个 Scope", nameof(scopes));
        AllowedScopes = scopes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>更新重定向 URI</summary>
    public void UpdateRedirectUris(HashSet<string> redirectUris)
    {
        if (redirectUris is null || redirectUris.Count == 0)
            throw new ArgumentException("至少需要一个 Redirect URI", nameof(redirectUris));
        RedirectUris = redirectUris;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>添加重定向 URI</summary>
    public void AddRedirectUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI 不能为空", nameof(uri));
        RedirectUris.Add(uri);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>移除重定向 URI</summary>
    public void RemoveRedirectUri(string uri)
    {
        if (!RedirectUris.Remove(uri))
            throw new InvalidOperationException($"Redirect URI '{uri}' 不存在");
        if (RedirectUris.Count == 0)
            throw new InvalidOperationException("不能移除所有 Redirect URI，至少保留一个");
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>更新 CORS 来源</summary>
    public void UpdateCorsOrigins(HashSet<string>? origins)
    {
        AllowedCorsOrigins = origins;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>禁用客户端</summary>
    public void Disable()
    {
        if (Status != ClientStatus.Active)
            throw new InvalidOperationException($"当前状态为 {Status}，无法禁用");
        Status = ClientStatus.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>启用客户端</summary>
    public void Enable()
    {
        if (Status != ClientStatus.Disabled)
            throw new InvalidOperationException($"当前状态为 {Status}，无法启用");
        Status = ClientStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>吊销客户端（密钥泄露时使用）</summary>
    public void Revoke()
    {
        Status = ClientStatus.Revoked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>验证给定的 redirect_uri 是否在白名单中</summary>
    public bool ValidateRedirectUri(string redirectUri)
    {
        return RedirectUris.Contains(redirectUri);
    }

    /// <summary>验证给定的 scope 是否在允许范围内</summary>
    public bool ValidateScope(string scope)
    {
        return AllowedScopes.Contains(scope);
    }

    /// <summary>验证给定的 grant_type 是否允许</summary>
    public bool ValidateGrantType(string grantType)
    {
        return AllowedGrantTypes.Contains(grantType);
    }

    /// <summary>是否为公共客户端（Public Client：SPA / Native / Desktop）</summary>
    public bool IsPublicClient => ApplicationType is ApplicationType.SPA
        or ApplicationType.Native
        or ApplicationType.Desktop;

    /// <summary>是否为机密客户端（Confidential Client：Web / Service）</summary>
    public bool IsConfidentialClient => !IsPublicClient;
}
