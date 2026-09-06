using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
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
    IOptionsSnapshot<JwtOptions> jwtOptions,
    ITokenSessionService tokenSessionService,
    IPermissionChecker permissionChecker)
    : IOAuthService
{
    private const string OAuthStateKeyPrefix = "oauth:state";
    private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(10);
    private const int OAuthStateLength = 32;

    /// <summary>OAuth 回调结果幂等缓存前缀（key=state，重复回调返回首次成功结果）</summary>
    private const string OAuthResultKeyPrefix = "oauth:result";
    private static readonly TimeSpan OAuthResultTtl = TimeSpan.FromMinutes(10);

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
            "microsoft" => await GenerateMicrosoftAuthUrl(redirectUri, state),
            "wechat" => GenerateWeChatAuthUrl(redirectUri, state),
            "qq" => GenerateQQAuthUrl(redirectUri, state),
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
                await userRepository.UnitOfWork.SaveChangesAsync();
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

        await userRepository.AddOneByUserAsync(newUser);

        // 4. 写入外部登录绑定（P6：与用户创建合并为一次提交，避免两步提交中途失败产生半成品用户）
        await EnsureExternalLoginAsync(providerType, externalUserInfo, newUser);
        await userRepository.UnitOfWork.SaveChangesAsync();

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
        await userRepository.UnitOfWork.SaveChangesAsync();

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
    /// 三项行为与原 RegisterByGitHubCommand 链路对齐：
    ///   1. 幂等性：以 state 为 key 缓存首次成功结果，重复回调（相同 state）直接返回缓存
    ///   2. RefreshToken：使用 BuildTokenAsync 获取完整 TokenResult，并缓存 access/refresh token
    ///   3. IsNewUser：返回是否新建用户，供 API 层发布 RegisterByUserIntegrationEvent
    /// </summary>
    public async Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri,
        string? state = null)
    {
        var normalizedProvider = provider.ToLowerInvariant();

        // ── 幂等：先查回调结果缓存（key=state），命中直接返回首次成功结果 ──
        if (!string.IsNullOrWhiteSpace(state))
        {
            var resultKey = $"{OAuthResultKeyPrefix}:{state}";
            var cached = await redisCacheService.StringGetAsync(resultKey);
            if (cached is not null)
            {
                logger.LogInformation("OAuth 回调幂等命中，返回首次成功结果：{Provider}", provider);
                return JsonSerializer.Deserialize<OAuthLoginResponse>(cached)!;
            }
        }

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

        var externalUserInfo = await GetExternalUserInfoAsync(normalizedProvider, code, redirectUri, state);

        var existingUser =
            await GetExistingUserByExternalLoginAsync(normalizedProvider, externalUserInfo.ProviderUserId);

        User user;
        var isNewUser = false;
        if (existingUser is not null)
        {
            user = existingUser;
            logger.LogInformation("Existing user logged in with {Provider}", provider);
        }
        else
        {
            user = await CreateOrUpdateUserFromExternalLoginAsync(normalizedProvider, externalUserInfo);
            isNewUser = true;
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

        // 权限集合 + 数据范围 claim（网关本地判定，与登录主链路一致；空权限也写空 claim → 网关本地拒绝）
        var permissions = await permissionChecker.GetUserPermissionsAsync(user.UserGuid);
        claims.Add(new Claim(PermissionClaimTypes.Permissions,
            string.Join(',', permissions.OrderBy(x => x, StringComparer.Ordinal))));
        var dataScope = await permissionChecker.GetUserDataScopeAsync(user.UserGuid);
        claims.Add(new Claim(PermissionClaimTypes.DataScope, dataScope.ToClaimValue()));

        // 使用 BuildTokenAsync 获取完整 TokenResult（含 RefreshToken），与原命令链路一致
        var tokenResult = await jwtTokenService.BuildTokenAsync(claims, _jwtOptions);

        // P3：多设备会话登记（与 UserService 一致，每设备独立槽位）
        await tokenSessionService.RegisterAsync(user.UserGuid, tokenResult,
            TimeSpan.FromSeconds(_jwtOptions.ExpireSeconds),
            TimeSpan.FromSeconds(_jwtOptions.RefreshTokenExpireSeconds));

        var response = new OAuthLoginResponse(
            tokenResult.AccessToken,
            tokenResult.RefreshToken ?? string.Empty,
            tokenResult.ExpiresAt,
            new UserInfo(
                user.UserGuid,
                user.UserEmail,
                user.UserName,
                user.AvatarUrl,
                user.UserRoleGuid.ToArray()
            ),
            isNewUser
        );

        // 幂等：缓存回调结果，重复回调（相同 state）返回首次成功结果
        if (!string.IsNullOrWhiteSpace(state))
        {
            await redisCacheService.StringSetAsync(
                $"{OAuthResultKeyPrefix}:{state}",
                JsonSerializer.Serialize(response),
                OAuthResultTtl);
        }

        return response;
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
        var clientId = _oauthOptions.GitHubOptions.ClientId;
        var query = $"client_id={Uri.EscapeDataString(clientId)}" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&state={Uri.EscapeDataString(state)}" +
                    "&scope=read:user%20user:email";
        return $"https://github.com/login/oauth/authorize?{query}";
    }

    /// <summary>
    /// 生成 Microsoft 授权链接。
    /// 公共客户端（未配置 ClientSecret）自动启用 PKCE（S256），code_verifier 与 state 关联存入 Redis，
    /// 回调换 token 时校验并消费。
    /// </summary>
    private async Task<string> GenerateMicrosoftAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.MicrosoftOptions;
        if (!options.Enable)
            throw new InvalidOperationException("Microsoft OAuth 未启用，请在 OAuthOptions:MicrosoftOptions:Enabled 中配置。");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("Microsoft OAuth 未配置 ClientId。");

        var query = new Dictionary<string, string>
        {
            { "client_id", options.ClientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", "openid profile email" },
            { "state", state },
            { "prompt", "select_account" }
        };

        // 未配置 ClientSecret → 公共客户端，必须使用 PKCE
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            var (verifier, challenge) = GeneratePkcePair();
            await redisCacheService.StringSetAsync(
                $"{OAuthStateKeyPrefix}:verifier:{state}", verifier, OAuthStateTtl);
            query["code_challenge"] = challenge;
            query["code_challenge_method"] = "S256";
        }

        return "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
               string.Join("&", query.Select(kv =>
                   $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
    }

    /// <summary>
    /// 生成微信扫码登录授权链接（开放平台网站应用，scope 固定 snsapi_login）。
    /// 注意：微信要求 redirect_uri 必须经过 URL 编码，且 URL 末尾追加 #wechat_redirect。
    /// </summary>
    private string GenerateWeChatAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.WeChatOptions;
        if (!options.Enabled)
            throw new InvalidOperationException("WeChat OAuth 未启用，请在 OAuthOptions:WeChatOptions:Enabled 中配置。");
        if (string.IsNullOrWhiteSpace(options.AppId))
            throw new InvalidOperationException("WeChat OAuth 未配置 AppId。");

        return "https://open.weixin.qq.com/connect/qrconnect" +
               $"?appid={Uri.EscapeDataString(options.AppId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               "&response_type=code" +
               "&scope=snsapi_login" +
               $"&state={Uri.EscapeDataString(state)}" +
               "#wechat_redirect";
    }

    /// <summary>
    /// 生成 QQ 互联授权链接（scope 固定 get_user_info）
    /// </summary>
    private string GenerateQQAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.QQOptions;
        if (!options.Enabled)
            throw new InvalidOperationException("QQ OAuth 未启用，请在 OAuthOptions:QQOptions:Enabled 中配置。");
        if (string.IsNullOrWhiteSpace(options.AppId))
            throw new InvalidOperationException("QQ OAuth 未配置 AppId。");

        return "https://graph.qq.com/oauth2.0/authorize" +
               "?response_type=code" +
               $"&client_id={Uri.EscapeDataString(options.AppId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               "&scope=get_user_info";
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers — OAuth 用户信息获取
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 根据 provider 路由到对应的 OAuth 用户信息获取方法
    /// </summary>
    private async Task<ExternalUserInfo> GetExternalUserInfoAsync(string provider, string code, string redirectUri,
        string? state = null)
    {
        return provider switch
        {
            "google" => await GetGoogleUserInfoAsync(code, redirectUri),
            "github" => await GetGitHubUserInfoAsync(code, redirectUri),
            "microsoft" => await GetMicrosoftUserInfoAsync(code, redirectUri, state),
            "wechat" => await GetWeChatUserInfoAsync(code, redirectUri),
            "qq" => await GetQQUserInfoAsync(code, redirectUri),
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
    /// 公共客户端（无 ClientSecret）使用授权时生成的 PKCE code_verifier 换 token。
    /// </summary>
    private async Task<ExternalUserInfo> GetMicrosoftUserInfoAsync(string code, string redirectUri, string? state)
    {
        var options = _oauthOptions.MicrosoftOptions;
        var client = httpClient.CreateClient();

        try
        {
            // Step 1: 用 code 换取 access_token
            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", options.ClientId },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" },
                { "scope", "openid profile email" }
            };

            // 机密客户端带 client_secret；公共客户端必须携带 PKCE code_verifier（关联 state，单次消费）
            if (!string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                tokenRequest["client_secret"] = options.ClientSecret;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(state))
                    throw new InvalidOperationException("缺少 OAuth state 参数，无法完成 PKCE 换码");

                var verifierKey = $"{OAuthStateKeyPrefix}:verifier:{state}";
                var verifier = await redisCacheService.StringGetAsync(verifierKey);
                if (string.IsNullOrEmpty(verifier))
                    throw new InvalidOperationException("PKCE code_verifier 缺失或已过期，请重新发起授权");
                tokenRequest["code_verifier"] = verifier;
                await redisCacheService.KeyDeleteAsync(verifierKey);
            }

            using var tokenResponse = await client.PostAsync(
                "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                new FormUrlEncodedContent(tokenRequest));
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorBody = await tokenResponse.Content.ReadAsStringAsync();
                logger.LogWarning("Microsoft token 交换失败：{StatusCode} {Body}",
                    (int)tokenResponse.StatusCode, errorBody);
                tokenResponse.EnsureSuccessStatusCode();
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            var accessToken = tokenData.GetProperty("access_token").GetString()!;

            // Step 2: 获取用户信息
            using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var userInfoResponse = await client.SendAsync(userRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var errorBody = await userInfoResponse.Content.ReadAsStringAsync();
                logger.LogWarning("Microsoft Graph /me 请求失败：{StatusCode} {Body}",
                    (int)userInfoResponse.StatusCode, errorBody);
                userInfoResponse.EnsureSuccessStatusCode();
            }

            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

            // 个人账号（MSA）无 mail 属性，回退 userPrincipalName（如 notcomd@outlook.com）
            var email = GetGraphString(userData, "mail")
                        ?? GetGraphString(userData, "userPrincipalName")
                        ?? throw new InvalidOperationException("Microsoft 用户信息缺少邮箱地址");

            return new ExternalUserInfo
            {
                ProviderUserId = userData.GetProperty("id").GetString()!,
                Email = email,
                UserName = GetGraphString(userData, "displayName"),
                // Graph /me 默认响应不含头像；如需可另行调用 /me/photo/$value
                AvatarUrl = null
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "获取 Microsoft 用户信息失败");
            throw;
        }
    }

    /// <summary>
    /// 读取 JsonElement 中的字符串属性，缺失或为空返回 null。
    /// </summary>
    private static string? GetGraphString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(value.GetString())
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// 生成 PKCE（RFC 7636 S256）对：verifier 为 32 字节随机数，challenge 为 verifier 的 SHA-256。
    /// </summary>
    private static (string Verifier, string Challenge) GeneratePkcePair()
    {
        var verifierBytes = RandomNumberGenerator.GetBytes(32);
        var verifier = Base64UrlEncode(verifierBytes);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return (verifier, Base64UrlEncode(challengeBytes));
    }

    /// <summary>
    /// URL 安全 Base64 编码（去填充、+ → -、/ → _）。
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// 获取微信用户信息（开放平台扫码登录）。
    /// Step 1: code 换 access_token（返回 openid）；Step 2: access_token + openid 取用户资料。
    /// 微信 userinfo 接口不返回邮箱，使用 {openid}@wechat.local 占位（RFC 2606 保留 .local，不会与真实邮箱冲突）。
    /// </summary>
    private async Task<ExternalUserInfo> GetWeChatUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.WeChatOptions;
        var client = httpClient.CreateClient();

        // Step 1: 用 code 换取 access_token + openid
        using var tokenResponse = await client.GetAsync(
            "https://api.weixin.qq.com/sns/oauth2/access_token" +
            $"?appid={Uri.EscapeDataString(options.AppId)}" +
            $"&secret={Uri.EscapeDataString(options.AppSecret)}" +
            $"&code={Uri.EscapeDataString(code)}" +
            "&grant_type=authorization_code");
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        EnsureWeChatSuccess(tokenData);

        var accessToken = tokenData.GetProperty("access_token").GetString()!;
        var openId = tokenData.GetProperty("openid").GetString()!;

        // Step 2: 获取用户资料
        using var userInfoResponse = await client.GetAsync(
            "https://api.weixin.qq.com/sns/userinfo" +
            $"?access_token={Uri.EscapeDataString(accessToken)}" +
            $"&openid={Uri.EscapeDataString(openId)}");
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
        EnsureWeChatSuccess(userData);

        var info = new ExternalUserInfo
        {
            ProviderUserId = openId,
            // 微信不返回邮箱：占位地址，避免与真实邮箱撞车
            Email = $"{openId}@wechat.local",
            UserName = userData.TryGetProperty("nickname", out var nickname) ? nickname.GetString() : null,
            AvatarUrl = userData.TryGetProperty("headimgurl", out var headImg) &&
                        !string.IsNullOrEmpty(headImg.GetString())
                ? new Uri(headImg.GetString()!)
                : null
        };

        // unionid：开放平台绑定后返回，用于跨应用唯一标识（存入 ProviderUnionId）
        if (userData.TryGetProperty("unionid", out var unionId) && !string.IsNullOrEmpty(unionId.GetString()))
            info.AdditionalClaims["unionid"] = unionId.GetString()!;

        return info;
    }

    /// <summary>
    /// 校验微信接口响应，失败时抛出带 errcode/errmsg 的异常
    /// </summary>
    private static void EnsureWeChatSuccess(JsonElement data)
    {
        if (data.TryGetProperty("errcode", out var errCode) && errCode.GetInt32() != 0)
            throw new InvalidOperationException(
                $"微信接口调用失败: errcode={errCode.GetInt32()}, errmsg={data.GetProperty("errmsg").GetString()}");
    }

    /// <summary>
    /// 获取 QQ 用户信息（互联网站应用）。
    /// Step 1: code 换 access_token（响应为表单文本而非 JSON）；
    /// Step 2: /oauth2.0/me 获取 openid（响应为 JSONP 包裹的 JSON）；
    /// Step 3: /user/get_user_info 获取昵称头像。
    /// QQ 接口不返回邮箱，使用 {openid}@qq.local 占位。
    /// </summary>
    private async Task<ExternalUserInfo> GetQQUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.QQOptions;
        var client = httpClient.CreateClient();

        // Step 1: 用 code 换取 access_token（QQ 返回 "access_token=xxx&expires_in=xxx" 表单文本）
        using var tokenResponse = await client.GetAsync(
            "https://graph.qq.com/oauth2.0/token" +
            "?grant_type=authorization_code" +
            $"&client_id={Uri.EscapeDataString(options.AppId)}" +
            $"&client_secret={Uri.EscapeDataString(options.AppKey)}" +
            $"&code={Uri.EscapeDataString(code)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}");
        tokenResponse.EnsureSuccessStatusCode();

        var tokenText = await tokenResponse.Content.ReadAsStringAsync();
        var tokenParams = HttpUtility.ParseQueryString(tokenText);
        var accessToken = tokenParams["access_token"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            var error = tokenParams["error"] ?? tokenParams["error_description"] ?? "未知错误";
            throw new InvalidOperationException($"QQ token 交换失败: {error}");
        }

        // Step 2: 获取 openid（响应为 callback( {...} ); 的 JSONP 包裹格式）
        var openId = await FetchQQOpenIdAsync(client, accessToken);

        // Step 3: 获取用户资料
        using var userInfoResponse = await client.GetAsync(
            "https://graph.qq.com/user/get_user_info" +
            $"?access_token={Uri.EscapeDataString(accessToken)}" +
            $"&oauth_consumer_key={Uri.EscapeDataString(options.AppId)}" +
            $"&openid={Uri.EscapeDataString(openId)}" +
            "&format=json");
        userInfoResponse.EnsureSuccessStatusCode();

        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
        if (userData.TryGetProperty("ret", out var ret) && ret.GetInt32() != 0)
        {
            var msg = userData.TryGetProperty("msg", out var m) ? m.GetString() : "未知错误";
            throw new InvalidOperationException($"QQ 用户信息获取失败: ret={ret.GetInt32()}, msg={msg}");
        }

        return new ExternalUserInfo
        {
            ProviderUserId = openId,
            // QQ 不返回邮箱：占位地址，避免与真实邮箱撞车
            Email = $"{openId}@qq.local",
            UserName = userData.TryGetProperty("nickname", out var nickname) ? nickname.GetString() : null,
            AvatarUrl = userData.TryGetProperty("figureurl_qq_2", out var avatar) &&
                        !string.IsNullOrEmpty(avatar.GetString())
                ? new Uri(avatar.GetString()!)
                : null
        };
    }

    /// <summary>
    /// 获取 QQ openid（解析 JSONP 包裹的 /oauth2.0/me 响应）
    /// </summary>
    private static async Task<string> FetchQQOpenIdAsync(HttpClient client, string accessToken)
    {
        using var meResponse = await client.GetAsync(
            "https://graph.qq.com/oauth2.0/me" +
            $"?access_token={Uri.EscapeDataString(accessToken)}");
        meResponse.EnsureSuccessStatusCode();

        var meText = await meResponse.Content.ReadAsStringAsync();

        // 响应形如: callback( {"client_id":"...","openid":"..."} );（JSONP 包裹），
        // 失败时形如: callback( {"error":100016,"error_description":"..."} );
        // 提取括号内的 JSON 再解析，避免依赖正则转义
        var jsonStart = meText.IndexOf('{');
        var jsonEnd = meText.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
            throw new InvalidOperationException($"QQ openid 获取失败: {meText[..Math.Min(meText.Length, 200)]}");

        using var doc = JsonDocument.Parse(meText[jsonStart..(jsonEnd + 1)]);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorCode) && errorCode.GetInt32() != 0)
        {
            var desc = root.TryGetProperty("error_description", out var d) ? d.GetString() : "未知错误";
            throw new InvalidOperationException($"QQ openid 获取失败: error={errorCode.GetInt32()}, {desc}");
        }

        if (!root.TryGetProperty("openid", out var openIdProp))
            throw new InvalidOperationException($"QQ openid 获取失败: 响应缺少 openid 字段");

        return openIdProp.GetString()!;
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

        var login = UserExternalLogin.Create(provider, info.ProviderUserId, info.UserName ?? info.Email,
            info.AdditionalClaims.TryGetValue("unionid", out var unionId) ? unionId : null);
        login.LinkUser(user.UserGuid);

        // P6：只登记变更，提交由调用方统一负责（避免多次独立提交破坏原子性）
        await userExternalLoginRepository.AddAsync(login);
    }
}
