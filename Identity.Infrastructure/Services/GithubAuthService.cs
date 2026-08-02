using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Identity.Domain.Dto.OAuth;
using Identity.Domain.Options;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Identity.Infrastructure.Services;

public class GithubAuthService(
    IUserExternalLoginRepository userExternalLoginRepository,
    IOptionsSnapshot<OAuthOptions> optionsSnapshot,
    IHttpClientFactory httpClientFactory) : IGitHubAuthService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, };

    public string GithubIndexAsync(string clientId)
    {
        var builder = new UriBuilder("https://github.com/login/oauth/authorize")
        {
            Query = $"client_id={Uri.EscapeDataString(clientId)}" + $"&state={Guid.CreateVersion7()}"
        };
        return builder.Uri.ToString();
    }

    public async Task<string> RedirectUriAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "no";

        var tokenData = await ExchangeCode(code);
        if (!tokenData.TryGetValue("access_token", out var tokenObj))
            return string.Empty;

        var token = tokenObj?.ToString();
        if (string.IsNullOrEmpty(token))
            return string.Empty;

        var userInfo = await GetByGithubUserInfo(token);
        if (!userInfo.TryGetValue("login", out var login) || !userInfo.TryGetValue("name", out var name))
            return string.Empty;

        var githubId = userInfo.TryGetValue("id", out var idObj)
            ? idObj?.ToString()
            : null;

        if (string.IsNullOrEmpty(githubId))
            return string.Empty;

        var displayName = name.ToString() ?? login.ToString() ?? string.Empty;
        var userExternalLogin = UserExternalLogin.Create(
            LoginProviderType.GitHub,
            githubId,
            displayName);

        userExternalLogin.UpdateTokens(token, null, null);

        await userExternalLoginRepository.AddAsync(userExternalLogin);

        return JsonSerializer.Serialize(userInfo, _jsonSerializerOptions);
    }

    public async Task<GitHubUserInfo?> GetGitHubUserAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;

        // ── 1. 用 code 换取 access_token ──
        var tokenData = await ExchangeCode(code);
        if (!tokenData.TryGetValue("access_token", out var tokenObj))
            return null;

        var token = tokenObj?.ToString();
        if (string.IsNullOrEmpty(token))
            return null;

        // ── 2. 获取 GitHub 用户基本信息 ──
        var userInfo = await GetByGithubUserInfo(token);
        if (!userInfo.TryGetValue("login", out var login) || !userInfo.TryGetValue("name", out var name))
            return null;

        var githubId = userInfo.TryGetValue("id", out var idObj)
            ? idObj?.ToString()
            : null;

        if (string.IsNullOrEmpty(githubId))
            return null;

        // ── 3. 获取用户邮箱 ──
        var email = await GetGitHubUserEmailAsync(token);

        // ── 4. 获取头像 URL ──
        var avatarUrl = userInfo.TryGetValue("avatar_url", out var avatarObj)
            ? avatarObj?.ToString()
            : null;

        return new GitHubUserInfo(
            githubId,
            login.ToString()!,
            name.ToString()!,
            email,
            avatarUrl,
            token);
    }

    public Task<bool> BindLinkUserAsync()
    {
        // F-07：显式降级——GitHub 账号绑定已由 OAuthService.LinkExternalLoginByCodeAsync / UnlinkExternalLoginFromUserAsync 提供真实链路，
        // 此遗留接口暂不接入额外绑定逻辑，返回 false 并保留 TODO 以对齐接口契约。
        // TODO(F-07): 若业务需要经此接口绑定，应委托 OAuthService 的绑定链路。
        return Task.FromResult(false);
    }

    /// <summary>
    /// 获取 GitHub 用户的主邮箱（优先 primary+verified，回退到任意 verified）
    /// </summary>
    private async Task<string?> GetGitHubUserEmailAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GitHubOAuthNotBlog", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var http = httpClientFactory.CreateClient();
            var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var emails = JsonSerializer.Deserialize<List<GitHubEmailInfo>>(json, _jsonSerializerOptions);

            var primary = emails?.FirstOrDefault(e => e.Primary && e.Verified)
                ?? emails?.FirstOrDefault(e => e.Verified);

            return primary?.Email;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<Dictionary<string, object>> GetByGithubUserInfo(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GitHubOAuthNotBlog", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var http = httpClientFactory.CreateClient();

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
               ?? new Dictionary<string, object>();
    }

    private async Task<Dictionary<string, object>> ExchangeCode(string code)
    {
        var clientId = optionsSnapshot.Value.GitHubOptions.ClientId;
        var clientSecret = optionsSnapshot.Value.GitHubOptions.ClientSecret;

        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var http = httpClientFactory.CreateClient();
        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
               ?? new Dictionary<string, object>();
    }

    private record GitHubEmailInfo(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified,
        [property: JsonPropertyName("visibility")] string? Visibility
    );
}