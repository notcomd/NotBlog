using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class GithubAuthApi
{
    public static RouteGroupBuilder GithubAuthApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/github");
        router.MapGet("/login/github", LoginAsync);
        router.MapGet("/github/callback", Callback);
        return router;
    }

    private static Task<string> LoginAsync(HttpContext httpContext, [FromServices] GithubAuthDI githubAuthDI)
    {
        var clientId = githubAuthDI.options.Value.GitHubOptions.ClientId;
        var redirectUri = githubAuthDI.GitHubAuthService.GithubIndexAsync(clientId);
        return Task.FromResult(redirectUri);
    }

    /// <summary>
    /// GitHub OAuth 回调：注册或登录（返回 JWT Token）
    /// </summary>
    private static async Task<IResult> Callback(
        [FromServices] GithubAuthDI githubAuthDi,
        [FromServices] INotMediator mediator,
        [FromQuery] string code,
        HttpContext httpContext)
    {
        if (string.IsNullOrEmpty(code))
            return Results.BadRequest(new { error = "授权码为空" });

        try
        {
            var command = new RegisterByGitHubCommand(code);
            var identityCommand = new IdentifiedCommand<RegisterByGitHubCommand, RegisterByGitHubResult>(
                IdentityApis.GetIdempotencyKey(httpContext), command);

            var result = await mediator.SendAsync(identityCommand);

            return Results.Ok(new
            {
                message = result.IsNewUser ? "注册成功" : "登录成功",
                isNewUser = result.IsNewUser,
                userId = result.UserId,
                userName = result.UserName,
                email = result.Email,
                avatarUrl = result.AvatarUrl,
                accessToken = result.Token.AccessToken,
                refreshToken = result.Token.RefreshToken,
                tokenType = result.Token.TokenType,
                expiresAt = result.Token.ExpiresAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "GitHub 登录失败",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static Task<bool> LinkGithubByUserAsync([FromServices] GithubAuthDI githubAuthDi,
        [FromBody] LinkUserRequest request)
    {
        // F-07：显式降级——GitHub 账号绑定请使用 OAuthApis 的 /oauth/link（真实链路）。
        // TODO(F-07): 若需要此遗留接口，应委托 OAuthService.LinkExternalLoginByCodeAsync。
        return Task.FromResult(false);
    }
}