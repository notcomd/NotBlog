using System.Net.Http.Headers;
using System.Text.Json.Serialization;
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
            Query = $"client_id={Uri.EscapeDataString(clientId)}" + $"state={Guid.CreateVersion7()}"
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

        // Parse GitHub user ID
        var githubId = userInfo.TryGetValue("id", out var idObj)
            ? idObj?.ToString()
            : null;

        if (string.IsNullOrEmpty(githubId))
            return string.Empty;

        var displayName = name.ToString() ?? login.ToString() ?? string.Empty;

        // Persist the external login record
        var userExternalLogin = UserExternalLogin.Create(
            LoginProviderType.GitHub,
            githubId,
            displayName);

        userExternalLogin.UpdateTokens(token, null, null);

        await userExternalLoginRepository.AddAsync(userExternalLogin);

        //var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, };
        return JsonSerializer.Serialize(userInfo, _jsonSerializerOptions);
    }

    public Task<bool> BindLinkUserAsync()
    {
        throw new NotImplementedException();
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
}