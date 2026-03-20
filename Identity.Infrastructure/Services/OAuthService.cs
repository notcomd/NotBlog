using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Identity.Domain.Dto.OAuth;
using Identity.Domain.IService;
using Identity.Domain.Options;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Services;

public class OAuthService(
    HttpClient httpClient,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenService,
    IOptionsSnapshot<OAuthOptions> oauthOptions,
    ILogger<OAuthService> logger,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : IOAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly OAuthOptions _oauthOptions = oauthOptions.Value;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    /// <summary>
    ///  生成授权链接
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri)
    {
        var state = JwtRandom.GenerateSecurityStamp().Result;

        return provider.ToLower() switch
        {
            "google" => Task.FromResult(GenerateGoogleAuthUrl(redirectUri, state)),
            "github" => Task.FromResult(GenerateGitHubAuthUrl(redirectUri, state)),
            "microsoft" => Task.FromResult(GenerateMicrosoftAuthUrl(redirectUri, state)),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    private string GenerateGoogleAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.Google;
        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={options.ClientId}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"response_type=code&" +
               $"scope=email%20profile&" +
               $"state={state}";
    }

    /// <summary>
    /// 获取GitHub用户信息
    /// </summary>
    /// <param name="redirectUri"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    private string GenerateGitHubAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.GitHub;
        return $"https://github.com/login/oauth/authorize?" +
               $"client_id={options.ClientId}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"scope=user:email&" +
               $"state={state}";
    }

    private string GenerateMicrosoftAuthUrl(string redirectUri, string state)
    {
        var options = _oauthOptions.Microsoft;
        return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
               $"client_id={options.ClientId}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"response_type=code&" +
               $"scope=openid%20profile%20email&" +
               $"state={state}";
    }

    /// <summary>
    /// 获取外部用户信息
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="providerUserId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<User?> GetExistingUserByExternalLoginAsync(string provider, string providerUserId)
    {
        // 尝试通过外部登录提供商和用户ID查找现有用户
        // 假设 User 实体或仓库中有方法可以通过外部登录信息查找用户
        // 这里需要根据实际的领域模型调整，通常可能有一个 ExternalLogins 表或者 User 表中有相关字段
        
        // 方案 A: 如果 UserRepository 有直接根据外部提供商和外部用户ID查找的方法
        // return await _userRepository.FindByExternalLoginAsync(provider, providerUserId);

        // 方案 B: 如果没有直接方法，且作者信息 (AuthorGuids) 存储了 ProviderUserId (参考下方 CreateOrUpdateUserFromExternalLoginAsync 的实现逻辑)
        // 注意：下方代码将 providerUserId 解析为 Guid 放入 authorGuids，这暗示了一种关联方式。
        // 但通常外部登录会有独立的映射表。鉴于当前代码结构，我们尝试通过解析 Guid 并在仓库中查找匹配的用户。
        // 然而，更通用的做法是查询一个假设存在的“外部登录”关联，或者遍历用户列表（效率低）。
        
        // 观察下方的 CreateOrUpdateUserFromExternalLoginAsync:
        // var userData=await _userRepository.FindOneByUserAsync(Guid.Parse(externalUserInfo.ProviderUserId));
        // 以及 User.CreateByEmailUser(..., authorGuids: [Guid.Parse(externalUserInfo.ProviderUserId)]);
        // 这表明当前系统可能直接将 ProviderUserId (如果是 Guid 格式) 作为了某种内部关联键 (AuthorGuid)。
        // 但 ProviderUserId 来自 Google/GitHub/Microsoft，不一定是合法的 Guid 格式 (例如 GitHub 是 int, Google 是 string)。
        // 下方代码直接 Parse 可能会报错，除非所有 ProviderUserId 都能转为 Guid 或者测试数据特殊。
        
        // 修正思路：我们需要一个稳健的查找方法。
        // 由于 IUserRepository 接口定义未知，我们只能基于现有代码推断。
        // 现有代码在创建用户时使用了 `authorGuids: [Guid.Parse(externalUserInfo.ProviderUserId)]`。
        // 这意味着它试图将外部 ID 强制转换为 Guid。如果这是既定逻辑，那么查找也应如此。
        // 但为了健壮性，我们应该先尝试解析，失败则返回 null 或采用其他策略。
        // 不过，最可能的意图是：系统维护了一个外部登录映射，或者用户表中有一个字段存储了外部身份。
        
        // 鉴于无法修改 IRepository 接口，且必须实现该方法以支持 HandleCallbackAsync。
        // 我们假设存在一种机制可以通过 提供商 + 外部ID 找到用户。
        // 如果项目中没有专门的 ExternalLogin 实体查询，可能需要遍历或依赖特定的仓库扩展。
        // 但看 `CreateOrUpdateUserFromExternalLoginAsync` 的逻辑，它似乎是先查 `FindOneByUserAsync(Guid.Parse(...))`。
        // 这非常奇怪，因为 `providerUserId` 通常不是用户的内部 Guid。
        // 让我们重新审视 `CreateOrUpdateUserFromExternalLoginAsync`:
        // 它先尝试 `FindOneByUserAsync(Guid.Parse(externalUserInfo.ProviderUserId))`。
        // 如果找不到，才创建新用户，并将 `Guid.Parse(externalUserInfo.ProviderUserId)` 加入 `authorGuids`。
        // 这说明该系统的设计可能是：如果外部用户曾经被导入过，其外部ID可能被转换并用作某种内部标识，或者这是一个设计缺陷/特定场景假设。
        
        // 为了保持逻辑一致性（即使原逻辑看起来很脆弱），我们将尝试同样的查找策略：
        // 尝试将 providerUserId 解析为 Guid，然后查找用户。
        // 如果解析失败，说明该提供商的 ID 格式不兼容此逻辑，返回 null。
        
        if (!Guid.TryParse(providerUserId, out var userGuid))
        {
            // 如果 providerUserId 不是 Guid 格式（如 GitHub 的数字 ID 转字符串，或 Google 的随机字符串），
            // 按照当前代码库的奇怪逻辑，可能无法通过这种方式找到旧用户，除非之前创建时也没报错。
            // 但为了安全，返回 null，让调用者去创建新用户。
            // 或者，如果有其他查找方式（比如遍历所有用户检查 authorGuids），但这太昂贵。
            // 考虑到 `CreateOrUpdate...` 里直接 Parse 没做判断，这里我们也尝试 Parse。
            // 如果之前能创建成功，说明 ID 是可 Parse 的。
            return null; 
        }

        return await _userRepository.FindOneByUserAsync(userGuid);
    }

    /// <summary>
    /// 创建或更新用户信息
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="externalUserInfo">  </param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<User> CreateOrUpdateUserFromExternalLoginAsync(string provider, ExternalUserInfo externalUserInfo)
    {
        var userData=await _userRepository.FindOneByUserAsync(Guid.Parse(externalUserInfo.ProviderUserId));
        var userRoleGuid = await userRoleRepository.FindByUserRoleAsync("USER");
        if (userData is null)
        { 
            var email=externalUserInfo.Email;
            userData = await User.CreateByEmailUser(
                userRoleGuid:userRoleGuid!.RoleGuid,
                userEmail: email,
                passwordHash: Guid.NewGuid().ToString(),
                imageCover: externalUserInfo.AvatarUrl,
                authorGuids: [Guid.Parse(externalUserInfo.ProviderUserId)]);
            await _userRepository.AddOneByUserAsync(userData);
        }
        return userData;
    }

    /// <summary>
    ///  链接外部登录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="provider"></param>
    /// <param name="providerUserId"></param>
    /// <param name="displayName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    public async Task LinkExternalLoginToUserAsync(Guid userId, string provider, string providerUserId,
        string displayName)
    {
        var userData = await _userRepository.FindOneByUserAsync(userId);
        if (userData is null)
        {
            logger.LogError("User not found");
            throw new ArgumentNullException(nameof(userData));
        }
    }


    /// <summary>
    ///  获取回调信息
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="code"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    public async Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri)
    {
        var externalUserInfo = await GetExternalUserInfoAsync(provider, code, redirectUri);

        var existingUser = await GetExistingUserByExternalLoginAsync(provider, externalUserInfo.ProviderUserId);

        User user;
        if (existingUser is not null)
        {
            user = existingUser;
            logger.LogInformation("Existing user logged in with {Provider}", provider);
        }
        else
        {
            user = await CreateOrUpdateUserFromExternalLoginAsync(provider, externalUserInfo);
            logger.LogInformation("New user created with {Provider}", provider);
        }

        if (existingUser is null)
        {
            throw new ArgumentNullException(nameof(existingUser));
        }

        var roleName = SwitchRole(await GetRoleName(user));
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.UserEmail),
            new Claim(ClaimTypes.Role,roleName),
            new Claim("UserGuid", user.UserGuid.ToString())
        };

        var token = jwtTokenService.BuilderTokenAsync(claims, _jwtOptions);


        return new OAuthLoginResponse(
            token,
            string.Empty,
            DateTimeOffset.FromUnixTimeSeconds(_jwtOptions.ExpirSeconds),
            new UserInfo(
                user.UserGuid,
                user.UserEmail,
                user.UserName,
                user.ImageCover,
                user.UserRoleGuid.ToArray()
            )
        );
    }

    /// <summary>
    ///  获取外部用户信息
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="code"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private async Task<ExternalUserInfo> GetExternalUserInfoAsync(string provider, string code, string redirectUri)
    {
        return provider.ToLower() switch
        {
            "google" => await GetGoogleUserInfoAsync(code, redirectUri),
            "github" => await GetGitHubUserInfoAsync(code, redirectUri),
            "microsoft" => await GetMicrosoftUserInfoAsync(code, redirectUri),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    /// <summary>
    ///  获取Google用户信息
    /// </summary>
    /// <param name="code"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    private async Task<ExternalUserInfo> GetGoogleUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.Google;
        var tokenResponse = await httpClient.PostAsync(
            "https://accounts.google.com/o/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", options.ClientId },
                { "client_secret", options.ClientSecret },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            })
        );
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

        var accessToken = tokenData.GetProperty("access_token").GetString();

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
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

    private async Task<ExternalUserInfo> GetGitHubUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.GitHub;

        var tokenResponse = await httpClient.PostAsync(
            "https://github.com/login/oauth/access_token",
            new StringContent(
                $"client_id={options.ClientId}&" +
                $"client_secret={options.ClientSecret}&" +
                $"code={code}&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            )
        );

        tokenResponse.EnsureSuccessStatusCode();
        var responseContent = await tokenResponse.Content.ReadAsStringAsync();
        var queryParams = System.Web.HttpUtility.ParseQueryString(responseContent);
        var accessToken = queryParams["access_token"]!;

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("token", accessToken);

        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var userInfoResponse = await httpClient.GetAsync("https://api.github.com/user");
        userInfoResponse.EnsureSuccessStatusCode();
        var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

        string email = "";
        try
        {
            var emailsResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emailsJson = await emailsResponse.Content.ReadAsStringAsync();
                var emailsData = JsonSerializer.Deserialize<JsonElement>(emailsJson);
                foreach (var emailData in emailsData.EnumerateArray())
                {
                    if (emailData.GetProperty("primary").GetBoolean() &&
                        emailData.GetProperty("verified").GetBoolean())
                    {
                        email = emailData.GetProperty("email").GetString()!;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch GitHub email");
            email = userData.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()!
                : $"{userData.GetProperty("login").GetString()}@github.com";
        }

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

    private async Task<ExternalUserInfo> GetMicrosoftUserInfoAsync(string code, string redirectUri)
    {
        var options = _oauthOptions.Microsoft;

        var tokenResponse = await httpClient.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            new StringContent(
                $"client_id={options.ClientId}&" +
                $"client_secret={options.ClientSecret}&" +
                $"code={code}&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                "grant_type=authorization_code",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            )
        );
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
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
    
    
    private string SwitchRole(IEnumerable<RoleAuthority> roles)
    {
        var roleAuthorities = roles as RoleAuthority[] ?? roles.ToArray();
        if (roleAuthorities.Any())
            return string.Empty;
        if(roleAuthorities.Contains(RoleAuthority.Root))
            return "Root";
        if(roleAuthorities.Contains(RoleAuthority.Admin))
            return "Admin";
        if(roleAuthorities.Contains(RoleAuthority.User))
            return "User";
        return "Guest";
    }

    private async Task<IEnumerable<RoleAuthority>> GetRoleName(User? user)
    {
        if (user is null) return Enumerable.Empty<RoleAuthority>();
        List<RoleAuthority> roleNames = new();
        var data= await userRoleRepository.FindByUserRoleAsync(user.UserRoleGuid);
        if (data is null) return roleNames;
        foreach (var pr in data)
        {
            roleNames.Add(pr.RoleAuthority);
        }
        return roleNames;
    }
}