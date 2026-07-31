namespace Identity.Domain.Dto.OAuth;

/// <summary>
/// GitHub OAuth 返回的用户信息
/// </summary>
public record GitHubUserInfo(
    string Id,
    string Login,
    string Name,
    string? Email,
    string? AvatarUrl,
    string AccessToken
);
