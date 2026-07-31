using Identity.Domain.Dto.OAuth;

namespace Identity.Domain.IService;

public interface IGitHubAuthService
{
    /// <summary>
    /// 生成 GitHub OAuth 授权跳转地址
    /// </summary>
    string GithubIndexAsync(string clientId);

    /// <summary>
    /// GitHub OAuth 回调：用 code 换取 access_token 并获取用户信息
    /// </summary>
    Task<string> RedirectUriAsync(string code);

    /// <summary>
    /// 获取 GitHub 用户信息（结构化返回值，用于注册/登录流程）
    /// </summary>
    Task<GitHubUserInfo?> GetGitHubUserAsync(string code);

    /// <summary>
    /// 绑定已有账号
    /// </summary>
    Task<bool> BindLinkUserAsync();
}