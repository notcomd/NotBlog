namespace Identity.Domain.Entities.UserAggregate;

public class UserExternalLogin : Entity
{
    protected UserExternalLogin()
    {
        CreatedAt = DateTimeOffset.UtcNow;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public UserExternalLogin(Guid userGuid, string loginProvider, string providerKey, string providerDisplayName)
        : this()
    {
        UserGuid = userGuid;
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
    }

    public Guid UserGuid { get; private set; }

    public string LoginProvider { get; private set; } = null!;

    public string ProviderKey { get; private set; } = null!;

    public string ProviderDisplayName { get; private set; } = null!;

    public string? AccessToken { get; private set; }

    public DateTimeOffset? AccessTokenExpiresAt { get; private set; }

    public string? RefreshToken { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    public static UserExternalLogin Create(
        Guid userGuid,
        string loginProvider,
        string providerKey,
        string providerDisplayName)
    {
        return new UserExternalLogin(userGuid, loginProvider, providerKey, providerDisplayName);
    }

    public void UpdateTokens(string? accessToken, DateTimeOffset? expiresAt, string? refreshToken)
    {
        AccessToken = accessToken;
        AccessTokenExpiresAt = expiresAt;
        RefreshToken = refreshToken;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void LinkToUser(Guid userGuid)
    {
        UserGuid = userGuid;
    }
}