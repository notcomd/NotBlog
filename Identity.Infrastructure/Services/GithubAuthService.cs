using System.Net.Http.Headers;
using Identity.Domain.Options;

namespace Identity.Infrastructure.Services;

public class GithubAuthService(
    IUserExternalLoginRepository userExternalLoginRepository,
    IOptionsSnapshot<OAuthOptions> optionsSnapshot,
    IHttpClientFactory httpClientFactory) : IGitHubAuthService
{
    public string GithubIndexAsync(string clientId)
    {
        UriBuilder builder = new UriBuilder(new Uri($"https://github.com/login/oauth/authorize?client_id={clientId}"));
        return builder.Uri.ToString();
    }

    public async Task<string> RedirectUriAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return "no";
        }

        var tokenData = await ExchangeCode(code);
        if (tokenData.TryGetValue("access_token", out var tokenobj))
        {
            return string.Empty;
        }

        var token = tokenobj.ToString();
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        var userInfo = await GetByGithubUserInfo(token);
        if (userInfo.TryGetValue("login", out var login) && userInfo.TryGetValue("name", out var name))
        {
            return string.Empty;
        }

        return "yes";
    }

    public Task<bool> BindLinkUserAsync()
    {
        throw new NotImplementedException();
    }

    private async Task<Dictionary<string, object>> GetByGithubUserInfo(string token)
    {
        using var http = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("GitHubOAuthNotBlog", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = response.Content.ReadAsStream();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    }

    private async Task<Dictionary<string, object>> ExchangeCode(string code)
    {
        var client_id = optionsSnapshot.Value.GitHub.ClientId;
        var client_secret = optionsSnapshot.Value.GitHub.ClientSecret;
        var scope = optionsSnapshot.Value.GitHub.Scope;
        using var http = httpClientFactory.CreateClient();
        var parameters = new Dictionary<string, string>
        {
            { "client_id", client_id },
            { "client_secret", client_secret },
            { "code", code },
            { "scope", string.Join(",", scope) }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.github.com/login/auth/access_token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var reponst = await http.SendAsync(request);
        reponst.EnsureSuccessStatusCode();
        var jsondat = await reponst.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, object>>(jsondat) ?? new Dictionary<string, object>();
    }

    private Task WriteGithubLoginAsync(UserExternalLogin userExternalLogin)
    {
        if (userExternalLogin is null)
        {
            return Task.CompletedTask;
        }

        var user = UserExternalLogin.Create(LoginProviderType.GitHub, userExternalLogin.ProviderKey,
            userExternalLogin.ProviderDisplayName);
        return Task.CompletedTask;
    }
}