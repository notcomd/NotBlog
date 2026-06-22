namespace Identity.Domain.Entities.UserExternalLoginAggregate;

/// <summary>
/// 用户外部登录映射实体
/// 
/// 关联本地用户与第三方 OAuth / 开放平台账号。
/// 每个实体记录一条外部登录绑定关系，一个本地用户可以有多个外部登录记录。
/// 
/// 支持的提供商: Google, Microsoft, GitHub, 微信, QQ
/// </summary>
public class UserExternalLogin : Entity, IAggregateRoot
{
    /// <summary>EF Core 无参构造函数</summary>
    protected UserExternalLogin()
    {
        LoginId = Guid.CreateVersion7();
        CreatedAt = DateTimeOffset.UtcNow;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>外部登录记录 ID</summary>
    public Guid LoginId { get; init; }

    /// <summary>关联的本地用户 ID</summary>
    public Guid UserId { get; private set; }

    /// <summary>登录提供商</summary>
    public LoginProviderType Provider { get; private set; }

    /// <summary>第三方平台的用户唯一标识
    /// Google: sub, GitHub: id, Microsoft: oid, 微信: openid</summary>
    public string ProviderKey { get; private set; } = null!;

    /// <summary>微信 unionid（跨应用唯一定位用户，微信生态专用）</summary>
    public string? ProviderUnionId { get; private set; }

    /// <summary>第三方显示名称</summary>
    public string ProviderDisplayName { get; private set; } = null!;

    /// <summary>加密后的访问令牌</summary>
    public string? EncryptedAccessToken { get; private set; }

    /// <summary>加密后的刷新令牌</summary>
    public string? EncryptedRefreshToken { get; private set; }

    /// <summary>Token 过期时间</summary>
    public DateTimeOffset? TokenExpiresAt { get; private set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>最后使用时间</summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    // ── 工厂方法 ──

    /// <summary>
    /// 创建新的外部登录记录
    /// </summary>
    /// <param name="provider">登录提供商</param>
    /// <param name="providerKey">第三方用户唯一标识</param>
    /// <param name="displayName">第三方显示名称</param>
    /// <param name="unionId">微信 unionid（可选）</param>
    public static UserExternalLogin Create(
        LoginProviderType provider,
        string providerKey,
        string displayName,
        string? unionId = null)
    {
        // Guard
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new ArgumentException("ProviderKey 不能为空", nameof(providerKey));

        return new UserExternalLogin
        {
            Provider = provider,
            ProviderKey = providerKey,
            ProviderUnionId = unionId,
            ProviderDisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName))
        };
    }

    // ── 领域行为 ──

    /// <summary>
    /// 更新 Token 信息（加密后的 Token 由上层服务传入）
    /// </summary>
    /// <param name="encryptedAccessToken">加密后的访问令牌</param>
    /// <param name="encryptedRefreshToken">加密后的刷新令牌（可选）</param>
    /// <param name="expiresAt">过期时间（可选）</param>
    public void UpdateTokens(
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
            throw new ArgumentException("AccessToken 不能为空", nameof(encryptedAccessToken));

        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        TokenExpiresAt = expiresAt;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 将外部登录绑定到本地用户
    /// </summary>
    /// <exception cref="InvalidOperationException">当该记录已绑定用户时抛出</exception>
    public void LinkUser(Guid userId)
    {
        if (UserId != Guid.Empty)
            throw new InvalidOperationException(
                $"外部登录记录已绑定用户: {UserId}，请先调用 UnlinkUser 解绑");

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId 不能为空", nameof(userId));

        UserId = userId;
    }

    /// <summary>
    /// 解绑本地用户
    /// </summary>
    public void UnlinkUser()
    {
        UserId = Guid.Empty;
    }

    /// <summary>
    /// 更新最后使用时间（无需完整登录时刷新）
    /// </summary>
    public void MarkAsUsed()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }
}