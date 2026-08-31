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


    public Guid NotClientId { get; init; } = Guid.CreateVersion7();

   
    public string ClientId { get; private set; } = null!;

   

   
    public string ApplicationName { get; private set; } = null!;

    
    public string? ApplicationDescription { get; private set; }

    
    public string? ApplicationIcon { get; private set; }

    
    public string? HomepageUri { get; private set; }

    
    public string? PrivacyPolicyUri { get; private set; }

    
    public string? TermsOfServiceUri { get; private set; }


    public string? ContactEmail { get; private set; }

  
    public string ClientSecret { get; private set; } = null!;

    
    public HashSet<string> RedirectUris { get; private set; } = [];

   
    public HashSet<string> PostLogoutRedirectUris { get; private set; } = [];

    
    public HashSet<string> AllowedScopes { get; private set; } = [];

    
    public HashSet<string> AllowedGrantTypes { get; private set; } = [];

    // ── 安全与类型 ──


    public ApplicationType ApplicationType { get; private set; }

   
    public string TokenEndpointAuthMethod { get; private set; } = null!;

    
    public bool RequirePkce { get; private set; }

   
    public bool RequireConsent { get; private set; }

    
    public HashSet<string>? AllowedCorsOrigins { get; private set; }

    // ── 状态与审计 ──

    
    public ClientStatus Status { get; private set; }

    
    public DateTimeOffset CreatedAt { get; init; }

   
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── 领域行为 ──

    
    public void RotateSecret(string newSecret)
    {
        if (string.IsNullOrWhiteSpace(newSecret))
            throw new ArgumentException("密钥不能为空", nameof(newSecret));
        ClientSecret = newSecret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    
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


    public void UpdateGrantTypes(HashSet<string> grantTypes)
    {
        if (grantTypes is null || grantTypes.Count == 0)
            throw new ArgumentException("至少需要一种 Grant Type", nameof(grantTypes));
        AllowedGrantTypes = grantTypes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }


    public void UpdateScopes(HashSet<string> scopes)
    {
        if (scopes is null || scopes.Count == 0)
            throw new ArgumentException("至少需要一个 Scope", nameof(scopes));
        AllowedScopes = scopes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }


    public void UpdateRedirectUris(HashSet<string> redirectUris)
    {
        if (redirectUris is null || redirectUris.Count == 0)
            throw new ArgumentException("至少需要一个 Redirect URI", nameof(redirectUris));
        RedirectUris = redirectUris;
        UpdatedAt = DateTimeOffset.UtcNow;
    }


    public void AddRedirectUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI 不能为空", nameof(uri));
        RedirectUris.Add(uri);
        UpdatedAt = DateTimeOffset.UtcNow;
    }


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

   
    public bool IsPublicClient => ApplicationType is ApplicationType.SPA
        or ApplicationType.Native
        or ApplicationType.Desktop;

    
    /// <summary>是否为机密客户端（Confidential Client：Web / Service）</summary>
    public bool IsConfidentialClient => !IsPublicClient;
}
