using Microsoft.Extensions.Options;

namespace Identity.Web.API.APIs;

public record GithubAuthDI(
    IUserExternalLoginRepository userExternalLoginRepository,
    IUserRepository userRepository,
    IGitHubAuthService GitHubAuthService,
    IOptionsSnapshot<OAuthOptions> options);