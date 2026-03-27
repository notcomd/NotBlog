using Identity.Domain.Dto.OAuth;
using Identity.Domain.IService;

namespace Identity.Web.API.APIs;

public static class OAuthApis
{
    public static RouteGroupBuilder MapOAuthEndpoints(this RouteGroupBuilder routeBuilder)
    {
        var oauth = routeBuilder.MapGroup("/oauth").WithTags("OAuth Authentication");

        oauth.MapGet("/providers", GetAvailableProviders)
            .WithName("GetOAuthProviders");

        oauth.MapPost("/login/init", InitOAuthLogin)
            .WithName("InitiateOAuthLogin");

        oauth.MapPost("/{provider}/callback", HandleOAuthCallback)
            .WithName("HandleOAuthCallback");

        oauth.MapPost("/link", LinkExternalLogin)
            .RequireAuthorization()
            .WithName("LinkExternalLogin");

        oauth.MapGet("/linked", GetLinkedAccounts)
            .RequireAuthorization()
            .WithName("GetLinkedAccounts");

        return routeBuilder;
    }

    private static IResult GetAvailableProviders()
    {
        var providers = new List<OAuthProviderInfo>
        {
            new("google", "Google", true),
            new("github", "GitHub", true),
            new("microsoft", "Microsoft", true)
        };

        return Results.Ok(providers);
    }

    private static async Task<IResult> InitOAuthLogin(
        OAuthLoginInitRequest request,
        IOAuthService oauthService)
    {
        try
        {
            var authUrl = await oauthService.GenerateAuthorizationUrlAsync(
                request.Provider,
                request.RedirectUri
            );

            return Results.Ok(new { AuthorizationUrl = authUrl });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleOAuthCallback(
        string provider,
        [FromBody] OAuthCallbackRequest request,
        IOAuthService oauthService)
    {
        try
        {
            var response = await oauthService.HandleCallbackAsync(
                provider,
                request.Code,
                request.RedirectUri
            );

            return Results.Ok(response);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "OAuth Provider Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Authentication Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> LinkExternalLogin(
        [FromBody] OAuthCallbackRequest request,
        IOAuthService oauthService,
        HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(c => c.Type == "user_id");
        if (userIdClaim is null)
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);

        try
        {
            var response = await oauthService.HandleCallbackAsync(
                request.Provider,
                request.Code,
                request.RedirectUri
            );

            await oauthService.LinkExternalLoginToUserAsync(
                userId,
                request.Provider,
                response.UserInfo.UserId.ToString(),
                request.Provider
            );

            return Results.Ok(new { message = "External account linked successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Link Account",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetLinkedAccounts(
        IOAuthService oauthService,
        HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(c => c.Type == "user_id");
        if (userIdClaim is null)
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await oauthService.GetExistingUserByExternalLoginAsync("", userId.ToString());

        if (user is null)
            return Results.NotFound();
        var linked = user.AuthorGuids.Contains(user.UserGuid);

        return Results.Ok(linked);
    }
}