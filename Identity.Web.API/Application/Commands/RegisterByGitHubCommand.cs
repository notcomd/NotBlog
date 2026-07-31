using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// GitHub OAuth 注册/登录命令
/// </summary>
public record RegisterByGitHubCommand(string Code)
    : IRequest<RegisterByGitHubResult>, ILoggableCommand
{
    public string IdProperty => nameof(Code);
    public string IdValue => Code;
}

/// <summary>
/// GitHub 注册/登录结果
/// </summary>
public record RegisterByGitHubResult(
    bool IsNewUser,
    TokenResult Token,
    string UserId,
    string UserName,
    string? Email,
    string? AvatarUrl
);
