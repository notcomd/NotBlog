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

    public static Task<string> LoginAsync(HttpContext httpContext, [FromServices] GithubAuthDI githubAuthDI)
    {
        var clientId = githubAuthDI.options.Value.GitHubOptions.ClientId;
        Console.WriteLine(clientId);
        var redirectUri = githubAuthDI.GitHubAuthService.GithubIndexAsync(clientId);
        return Task.FromResult(redirectUri);
    }

    public static async Task<ActionResult<string>> Callback([FromServices] GithubAuthDI githubAuthDI,
        [FromQuery] string code)
    {
        Console.WriteLine($"我调用的该函数code: {code}");
        var data = await githubAuthDI.GitHubAuthService.RedirectUriAsync(code);
        return new ActionResult<string>(data);
    }
}