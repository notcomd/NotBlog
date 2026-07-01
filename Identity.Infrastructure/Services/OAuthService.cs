using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Identity.Domain.Dto.OAuth;
using Identity.Domain.Options;
using Notcomd.Token.JWT.Security;

namespace Identity.Infrastructure.Services;

public class OAuthService(
    IHttpClientFactory httpClient,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenService,
    IOptionsSnapshot<OAuthOptions> oauthOptions,
    ILogger<OAuthService> logger,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : IOAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly OAuthOptions _oauthOptions = oauthOptions.Value;

    /// <summary>
    /// 生成 OAuth 授权链接
    /// </summary>
    /// <param name="provider">提供商（google / github / microsoft）</param>
    /// <param name="redirectUri">回调地址</param>
    /// <returns>授权 URL</returns>
    /// <exception cref="ArgumentException">不支持的 provider 时抛出</exception>
    public async Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri)
    {
        var state = await JwtRandom.GenerateSecurityStamp();
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
    /// 先将 providerUserId 解析为 Guid 后通过仓库查询。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="providerUserId">提供商侧用户 ID</param>
    /// <returns>用户实体，未找到返回 null</returns>
    public async Task<User?> GetExistingUserByExternalLoginAsync(string provider, string providerUserId)
    {
        if (!Guid.TryParse(providerUserId, out var userGuid))
            return null;

        return await userRepository.FindOneByUserAsync(userGuid);
    }

    /// <summary>
    /// 根据外部用户信息创建或更新用户。
    /// 若用户已存在则直接返回，否则创建新用户并写入仓库。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="externalUserInfo">外部用户信息</param>
    /// <returns>用户实体</returns>
    /// <exception cref="FormatException">providerUserId 不是合法 Guid 时抛出</exception>
    public async Task<User> CreateOrUpdateUserFromExternalLoginAsync(string provider, ExternalUserInfo externalUserInfo)
    {
        if (!Guid.TryParse(externalUserInfo.ProviderUserId, out var userGuid))
            throw new FormatException($"ProviderUserId '{externalUserInfo.ProviderUserId}' 不是有效的 Guid 格式。");

        var userData = await userRepository.FindOneByUserAsync(userGuid);
        if (userData is not null)
            return userData;

        var userRole = await userRoleRepository.FindByUserRoleAsync("USER")
                       ?? throw new InvalidOperationException("默认角色 'USER' 未在数据库中配置。");

        userData = await User.CreateByEmailUser(
            userRoleGuid: userRole.RoleGuid,
            userEmail: externalUserInfo.Email,
            passwordHash: Guid.NewGuid().ToString(),
            imageCover: externalUserInfo.AvatarUrl,
            authorGuids: [userGuid]);

        await userRepository.AddOneByUserAsync(userData);
        return userData;
    }

    /// <summary>
    /// 将外部登录关联到指定用户。
    /// 当前为占位实现，仅校验用户是否存在。
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

        // TODO: 实现外部登录关联逻辑（将 provider + providerUserId 写入 ExternalLogins 表）
        logger.LogInformation("External login placeholder: {Provider} for user {UserId}", provider, userId);
    }

    /// <summary>
    /// 解除外部登录与指定用户的关联。
    /// 当前为占位实现。
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="provider">提供商</param>
    /// <param name="providerUserId">提供商侧用户 ID</param>
    /// <returns>成功返回 true</returns>
    public Task UnlinkExternalLoginFromUserAsync(Guid userId, string provider, string providerUserId)
    {
        // TODO: 实现解除外部登录关联逻辑
        logger.LogInformation("External login unlink placeholder: {Provider} for user {UserId}", provider, userId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理 OAuth 回调：获取外部用户信息、查找或创建本地用户、生成 JWT Token。
    /// </summary>
    /// <param name="provider">提供商</param>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">回调地址</param>
    /// <returns>登录响应</returns>
    public async Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri)
    {
        var normalizedProvider = provider.ToLowerInvariant();
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
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.UserEmail),
            new(ClaimTypes.Role, roleName),
            new("UserGuid", user.UserGuid.ToString())
        };

        var token = jwtTokenService.BuilderTokenAsync(claims, _jwtOptions);

        return new OAuthLoginResponse(
            token,
            string.Empty,
            DateTimeOffset.FromUnixTimeSeconds(_jwtOptions.ExpireSeconds),
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
               $"&state={state}";
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
               $"&state={state}";
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

        var roles = await userRoleRepository.FindByUserRoleAsync(user.UserRoleGuid);
        if (roles is null || roles.Count == 0)
            return Array.Empty<RoleAuthority>();

        return roles.Select(r => r.RoleAuthority).ToList();
    }
}