using System.Net.Http.Headers;
using System.Text;
using System.Web;
using CacheMemory.Core;
using Identity.Domain.Dto.OAuth;
using Identity.Domain.Options;
using Notcomd.Token.JWT.Security;

namespace Identity.Infrastructure.Services;

public class OAuthService(
    IHttpClientFactory httpClient,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IUserExternalLoginRepository userExternalLoginRepository,
    IRedisCacheService redisCacheService,
    IJwtTokenService jwtTokenService,
    IOptionsSnapshot<OAuthOptions> oauthOptions,
    ILogger<OAuthService> logger,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : IOAuthService
{
    private const string OAuthStateKeyPrefix = "oauth:state";
    private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(10);
    private const int OAuthStateLength = 32;

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly OAuthOptions _oauthOptions = oauthOptions.Value;

    /// <summary>
    /// 生成 OAuth 授权链接
    /// </summary>
    /// <param name="provider">提供商（google / github / microsoft）</param>
    /// <param name="redirectUri">回调地址</param>
    /// <returns>授权 URL</returns>
    /// <exception cref="ArgumentException">不支持的 provider 或 redirect_uri 不在白名单时抛出</exception>
    public async Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri)
    {
        ValidateRedirectUri(redirectUri);

        // S-11：state 防 CSRF —— 生成随机 state 并存入 Redis（TTL 10 分钟），回调时校验并删除
        var state = JwtRandom.GenerateRandomString(OAuthStateLength);
        await redisCacheService.StringSetAsync(
            $"{OAuthStateKeyPrefix}:{state}",
            provider.ToLowerInvariant(),
            OAuthStateTtl);

        var normalizedProvider = provider.ToLowerInvariant();

        return normalizedProvider switch
        {
            "google" => GenerateGoogleAuthUrl(redirectUri, state),
            "github" => GenerateGitHubAuthUrl(redirectUri, state),
            "microsoft" => GenerateMicrosoftAuthUrl(redirectUri, state),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    /// <summary>
    /// 通过外部登录信息查找已有用户。
    /// 按 provider + providerUserId（字符串）查询外部登录绑定，再映射到本地用户。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="providerUserId">提供商侧用户 ID（可为数字字符串，如 GitHub id）</param>
    /// <returns>用户实体，未找到返回 null</returns>
    public async Task<User?> GetExistingUserByExternalLoginAsync(string provider, string providerUserId)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId))
            return null;

        var providerType = ParseProvider(provider);
        var externalLogin = await userExternalLoginRepository.FindByProviderAsync(providerType, providerUserId);
        if (externalLogin is null || externalLogin.UserId == Guid.Empty)
            return null;

        return await userRepository.FindOneByUserAsync(externalLogin.UserId);
    }

    /// <summary>
    /// 根据外部用户信息创建或更新用户。
    /// 优先按外部登录绑定（provider + providerUserId）查找，其次按邮箱匹配，
    /// 均未命中时创建新用户（本地 Guid 与第三方 ID 无关），并落库 + 写入外部登录绑定。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="externalUserInfo">外部用户信息</param>
    /// <returns>用户实体</returns>
    public async Task<User> CreateOrUpdateUserFromExternalLoginAsync(string provider, ExternalUserInfo externalUserInfo)
    {
        var providerType = ParseProvider(provider);
        if (string.IsNullOrWhiteSpace(externalUserInfo.ProviderUserId))
            throw new ArgumentException("外部用户 ID 不能为空", nameof(externalUserInfo));

        // 1. 按 provider + providerUserId（字符串）查外部登录绑定，避免把数字 ID 当 Guid 解析
        var existingLogin = await userExternalLoginRepository.FindByProviderAsync(providerType, externalUserInfo.ProviderUserId);
        if (existingLogin is not null && existingLogin.UserId != Guid.Empty)
        {
            var linked = await userRepository.FindOneByUserAsync(existingLogin.UserId);
            if (linked is not null)
                return linked;
        }

        // 2. 按邮箱匹配已有用户
        if (!string.IsNullOrWhiteSpace(externalUserInfo.Email))
        {
            var byEmail = await userRepository.FindOneByUserAsync(externalUserInfo.Email);
            if (byEmail is not null)
            {
                await EnsureExternalLoginAsync(providerType, externalUserInfo, byEmail);
                return byEmail;
            }
        }

        // 3. 创建新用户（本地 Guid 由系统生成，与第三方 ID 无关）
        var userRole = await userRoleRepository.FindByUserRoleAsync("User")
                       ?? throw new InvalidOperationException("默认角色 'User' 未在数据库中配置。");

        var newUser = await User.CreateByEmailUser(
            userRoleGuid: userRole.RoleGuid,
            userEmail: externalUserInfo.Email,
            passwordHash: Guid.NewGuid().ToString(),
            imageCover: externalUserInfo.AvatarUrl,
            authorGuids: null);

        // S-11：添加后必须落库，否则用户不持久化
        await userRepository.AddOneByUserAsync(newUser);
        await userRepository.UnitOfWork.SaveChangesAsync();

        // 4. 写入外部登录绑定并保存
        await EnsureExternalLoginAsync(providerType, externalUserInfo, newUser);

        return newUser;
    }

    /// <summary>
    /// 将外部登录关联到指定用户。
    /// 写入 UserExternalLogin 绑定记录（Provider + ProviderKey 唯一）。
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="provider">提供商</param>
    /// <param name="providerUserId">提供商侧用户 ID</param>
    /// <param name="displayName">显示名</param>
    /// <exception cref="ArgumentException">用户不存在时抛出</exception>
    public async Task LinkExternalLoginToUserAsync(Guid userId, string provider, string providerUserId,
        string displayName)
    {
        var userData = await userRepository.FindOneByUserAsync(userId)
                       ?? throw new ArgumentException($"User {userId} not found.", nameof(userId));

        await EnsureExternalLoginAsync(ParseProvider(provider), new ExternalUserInfo
        {
            ProviderUserId = providerUserId,
            Email = userData.UserEmail,
            UserName = userData.UserName ?? userData.UserEmail
        }, userData);

        logger.LogInformation("External login linked: {Provider} for user {UserId}", provider, userId);
    }

    /// <summary>
    /// 解除外部登录与指定用户的关联（F-07：真实删除绑定记录）。
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="provider">提供商</param>
    /// <param name="providerUserId">提供商侧用户 ID</param>
    /// <exception cref="InvalidOperationException">未找到该用户的绑定记录时抛出</exception>
    public async Task UnlinkExternalLoginFromUserAsync(Guid userId, string provider, string providerUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId 不能为空", nameof(userId));

        var providerType = ParseProvider(provider);
        var externalLogin = await userExternalLoginRepository.FindByProviderAsync(providerType, providerUserId);
        if (externalLogin is null || externalLogin.UserId != userId)
            throw new InvalidOperationException("未找到该用户对应的外部登录绑定，无法解绑");

        await userExternalLoginRepository.DeleteAsync(externalLogin);
        await userExternalLoginRepository.UnitOfWork.SaveChangesAsync();

        logger.LogInformation("External login unlinked: {Provider} for user {UserId}", provider, userId);
    }

    /// <summary>
    /// 通过 OAuth 授权码将外部账号绑定到当前用户（F-07）。
    /// </summary>
    /// <param name="userId">本地用户 ID（从已认证的 NameIdentifier Claim 获取）</param>
    /// <param name="provider">提供商（google / github / microsoft）</param>
    /// <param name="code">OAuth 授权码</param>
    /// <param name="redirectUri">回调地址（须在白名单内）</param>
    public async Task LinkExternalLoginByCodeAsync(Guid userId, string provider, string code, string redirectUri)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId 不能为空", nameof(userId));

        var externalUserInfo = await GetExternalUserInfoAsync(provider.ToLowerInvariant(), code, redirectUri);
        await LinkExternalLoginToUserAsync(
            userId, provider, externalUserInfo.ProviderUserId, externalUserInfo.UserName ?? provider);
    }

    /// <summary>
    /// 获取用户已绑定的外部登录账号列表（F-07）。
    /// </summary>
    public async Task<IReadOnlyList<UserExternalLogin>> GetLinkedAccountsAsync(Guid userId)
    {
        return await userExternalLoginRepository.FindByUserIdAsync(userId);
    }

    /// <summary>
    /// 处理 OAuth 回调：校验 state 与 redirect_uri、获取外部用户信息、查找或创建本地用户、生成 JWT Token。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">回调地址</param>
    /// <param name="state">OAuth state（CSRF 校验，为空或与 Redis 中不匹配则拒绝）</param>
    /// <returns>登录响应</returns>
    public async Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri,
        string? state = null)
    {
        var normalizedProvider = provider.ToLowerInvariant();

        // S-11：state 防 CSRF —— 校验存在且匹配，校验通过后立即删除（单次使用）
        if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("缺少 OAuth state 参数，请求已被拒绝");

        var stateKey = $"{OAuthStateKeyPrefix}:{state}";
        var storedProvider = await redisCacheService.StringGetAsync(stateKey);
        if (storedProvider is null || !string.Equals(storedProvider, normalizedProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("OAuth state 校验失败，请求已被拒绝");

        await redisCacheService.KeyDeleteAsync(stateKey);

        // S-11：回调 redirect_uri 白名单校验
        ValidateRedirectUri(redirectUri);

        var externalUserInfo = await GetExternalUserInfoAsync(normalizedProvider, code, redirectUri);

        var existingUser =
            await GetExistingUserByExternalLoginAsync(normalizedProvider, externalUserInfo.ProviderUserId);

        User user;
        if (existingUser is not null)
        {
            user = existingUser;
            logger.LogInformation("Existing user logged in with {Provider}", provider);
        }
        else
        {
            user = await CreateOrUpdateUserFromExternalLoginAsync(normalizedProvider, externalUserInfo);
            logger.LogInformation("New user created with {Provider}", provider);
        }

        var roles = await GetUserRolesAsync(user);
        var roleName = DetermineHighestRole(roles);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserGuid.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.UserEmail),
            new(ClaimTypes.Role, roleName),
            new("UserGuid", user.UserGuid.ToString())
        };

        var token = jwtTokenService.BuilderTokenAsync(claims, _jwtOptions);

        return new OAuthLoginResponse(
            token,
            string.Empty,
            DateTimeOffset.UtcNow.AddSeconds(_jwtOptions.ExpireSeconds),
            new UserInfo(
                user.UserGuid,
                user.UserEmail,
                user.UserName,
                user.ImageCover,
                user.UserRoleGuid.ToArray()
            )
        );
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers — OAuth URL 生成
    // ═══════════════════════════════════════════════════════════

    private string GenerateGoogleAuthUrl(string redirectUri, string state)
    {
        return "https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={_oauthOptions.GoogleOptions.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               "&response_type=code" +
               "&scope=email%20profile" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    private string GenerateGitHubAuthUrl(string redirectUri, string state)
    {
        return string.Empty;
    }

    private string GenerateMicrosoftAuthUrl(string redirectUri, string state)
    {
        return "https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
               $"?client_id={_oauthOptions.MicrosoftOptions.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               "&response_type=code" +
               "&scope=openid%20profile%20email" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers — OAuth 用户信息获取
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 根据 provider 路由到对应的 OAuth 用户信息获取方法
    /// </summary>
    private async Task<ExternalUserInfo> GetExternalUserInfoAsync(string provider, string code, string redirectUri)
    {
        return provider switch
        {
            "google" => await GetGoogleUserInfoAsync(code, redirectUri),
            "github" => await GetGitHubUserInfoAsync(code, redirectUri),
            "microsoft" => await GetMicrosoftUserInfoAsync(code, redirectUri),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    private async Task<ExternalUserInfo> GetGoogleUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.GoogleOptions;
        var client = httpClient.CreateClient();

        // Step 1: 用 code 换取 access_token
        using var tokenResponse = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", options.ClientId },
                { "client_secret", options.ClientSecret },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            }));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // Step 2: 用 access_token 获取用户信息
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await client.SendAsync(request);
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        return new ExternalUserInfo
        {
            ProviderUserId = userData.GetProperty("id").GetString()!,
            Email = userData.GetProperty("email").GetString()!,
            UserName = userData.GetProperty("name").GetString(),
            AvatarUrl = userData.TryGetProperty("picture", out var picture)
                ? new Uri(picture.GetString()!)
                : null
        };
    }

    /// <summary>
    /// 获取 GitHub 用户信息。先拿 token，再拿 profile + emails。
    /// </summary>
    private async Task<ExternalUserInfo> GetGitHubUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.GitHubOptions;
        var client = httpClient.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Step 1: 用 code 换取 access_token
        var tokenRequestBody = $"client_id={options.ClientId}" +
                               $"&client_secret={options.ClientSecret}" +
                               $"&code={code}" +
                               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
            "https://github.com/login/oauth/access_token")
        {
            Content = new StringContent(tokenRequestBody, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        using var tokenResponse = await client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var responseContent = await tokenResponse.Content.ReadAsStringAsync();
        var queryParams = HttpUtility.ParseQueryString(responseContent);
        var accessToken = queryParams["access_token"]!;

        // Step 2: 获取用户 Profile
        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);

        using var userInfoResponse = await client.SendAsync(profileRequest);
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        // Step 3: 获取邮箱（优先选 primary + verified）
        var email = await FetchGitHubPrimaryEmailAsync(client, accessToken, userData);

        return new ExternalUserInfo
        {
            ProviderUserId = userData.GetProperty("id").GetInt32().ToString(),
            Email = email,
            UserName = userData.GetProperty("login").GetString(),
            AvatarUrl = userData.TryGetProperty("avatar_url", out var avatarUrl)
                ? new Uri(avatarUrl.GetString()!)
                : null
        };
    }

    /// <summary>
    /// 获取 GitHub 用户的主验证邮箱。失败时回退到 profile 中的 public_email 或拼接 login@github.com。
    /// </summary>
    private async ValueTask<string> FetchGitHubPrimaryEmailAsync(
        HttpClient client, string accessToken, JsonElement userData)
    {
        try
        {
            using var emailRequest = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/user/emails");
            emailRequest.Headers.Authorization = new AuthenticationHeaderValue("token", accessToken);

            using var emailsResponse = await client.SendAsync(emailRequest);
            if (!emailsResponse.IsSuccessStatusCode)
                goto Fallback;

            var emailsJson = await emailsResponse.Content.ReadAsStringAsync();
            var emailsData = JsonSerializer.Deserialize<JsonElement>(emailsJson);

            foreach (var emailData in emailsData.EnumerateArray())
            {
                if (emailData.GetProperty("primary").GetBoolean() &&
                    emailData.GetProperty("verified").GetBoolean())
                {
                    return emailData.GetProperty("email").GetString()!;
                }
            }

            Fallback:
            if (userData.TryGetProperty("email", out var emailProp) &&
                !string.IsNullOrEmpty(emailProp.GetString()))
            {
                return emailProp.GetString()!;
            }

            return $"{userData.GetProperty("login").GetString()}@github.com";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch GitHub primary email, using fallback.");
            return $"{userData.GetProperty("login").GetString()}@github.com";
        }
    }

    /// <summary>
    /// 获取 Microsoft 用户信息（通过 Microsoft Graph API）。
    /// </summary>
    private async Task<ExternalUserInfo> GetMicrosoftUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.MicrosoftOptions;
        var client = httpClient.CreateClient();

        // Step 1: 用 code 换取 access_token
        var tokenRequestBody = $"client_id={options.ClientId}" +
                               $"&client_secret={options.ClientSecret}" +
                               $"&code={code}" +
                               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                               "&grant_type=authorization_code";

        using var tokenResponse = await client.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            new StringContent(tokenRequestBody, Encoding.UTF8, "application/x-www-form-urlencoded"));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // Step 2: 获取用户信息
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await client.SendAsync(userRequest);
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        return new ExternalUserInfo
        {
            ProviderUserId = userData.GetProperty("id").GetString()!,
            Email = userData.GetProperty("mail").GetString()!,
            UserName = userData.GetProperty("displayName").GetString(),
            AvatarUrl = userData.TryGetProperty("avatar_url", out var avatarUrl)
                ? new Uri(avatarUrl.GetString()!)
                : null
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers — 角色处理
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 按优先级从高到低确定用户最高角色：Root > Admin > User > Guest
    /// </summary>
    private static string DetermineHighestRole(IReadOnlyCollection<RoleAuthority> roles)
    {
        if (roles.Count == 0)
            return "Guest";

        if (roles.Contains(RoleAuthority.Root))
            return "Root";
        if (roles.Contains(RoleAuthority.Admin))
            return "Admin";
        if (roles.Contains(RoleAuthority.User))
            return "User";
        return "Guest";
    }

    /// <summary>
    /// 查询用户的所有角色权限
    /// </summary>
    private async Task<IReadOnlyCollection<RoleAuthority>> GetUserRolesAsync(User? user)
    {
        if (user is null)
            return Array.Empty<RoleAuthority>();

        var roles = await userRoleRepository.FindByUserRoleAsync(user.UserRoleGuid.ToHashSet());
        if (roles is null || roles.Count == 0)
            return Array.Empty<RoleAuthority>();

        return roles.Select(r => r.RoleAuthority).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers — S-11 安全校验 / 外部登录绑定
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 将 provider 字符串映射为 <see cref="LoginProviderType"/>。
    /// </summary>
    private static LoginProviderType ParseProvider(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => LoginProviderType.Google,
        "github" => LoginProviderType.GitHub,
        "microsoft" => LoginProviderType.Microsoft,
        "wechat" => LoginProviderType.WeChat,
        "qq" => LoginProviderType.QQ,
        _ => throw new ArgumentException($"Unsupported provider: {provider}")
    };

    /// <summary>
    /// 校验 redirect_uri 是否在白名单内（S-11）。
    /// 白名单为空时不校验（向后兼容）；否则要求精确匹配或以 "*" 结尾的前缀匹配。
    /// </summary>
    private void ValidateRedirectUri(string redirectUri)
    {
        var whitelist = _oauthOptions.AllowedRedirectUris;
        if (whitelist is null || whitelist.Length == 0)
            return;

        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("redirect_uri 不能为空");

        var allowed = whitelist.Any(entry =>
        {
            var item = entry.Trim();
            if (item.EndsWith('*'))
                return redirectUri.StartsWith(item[..^1], StringComparison.OrdinalIgnoreCase);

            return string.Equals(redirectUri, item, StringComparison.OrdinalIgnoreCase);
        });

        if (!allowed)
            throw new ArgumentException($"redirect_uri '{redirectUri}' 不在允许的白名单中");
    }

    /// <summary>
    /// 确保 provider + providerUserId 的外部登录绑定存在（幂等）。
    /// </summary>
    private async Task EnsureExternalLoginAsync(LoginProviderType provider, ExternalUserInfo info, User user)
    {
        if (string.IsNullOrWhiteSpace(info.ProviderUserId))
            return;

        var existing = await userExternalLoginRepository.FindByProviderAsync(provider, info.ProviderUserId);
        if (existing is not null)
            return;

        var login = UserExternalLogin.Create(provider, info.ProviderUserId, info.UserName ?? info.Email);
        login.LinkUser(user.UserGuid);

        await userExternalLoginRepository.AddAsync(login);
        await userExternalLoginRepository.UnitOfWork.SaveEntitiesAsync();
    }
}
