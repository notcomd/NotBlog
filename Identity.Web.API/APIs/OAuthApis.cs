using System.Security.Claims;
using Identity.Domain.Dto.OAuth;
using Microsoft.AspNetCore.Mvc;

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

        // F-07：账号解绑真实链路（需认证）
        oauth.MapDelete("/unlink", UnlinkExternalLogin)
            .RequireAuthorization()
            .WithName("UnlinkExternalLogin");

        return routeBuilder;
    }

    /// <summary>
    /// S-13：统一 userId Claim 为 NameIdentifier（不再信任 user_id / user_guid 残留 Claim）
    /// </summary>
    private static Guid? TryGetAuthenticatedUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
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
        var userId = TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            // F-07：真实绑定链路——用授权码换取外部用户信息并写入 UserExternalLogin 绑定记录
            await oauthService.LinkExternalLoginByCodeAsync(
                userId.Value, request.Provider, request.Code, request.RedirectUri);

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
        var userId = TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        // F-07：返回当前用户真实的绑定列表
        var linked = await oauthService.GetLinkedAccountsAsync(userId.Value);
        var result = linked.Select(x => new
        {
            provider = x.Provider.ToString(),
            providerUserId = x.ProviderKey,
            displayName = x.ProviderDisplayName,
            linkedAt = x.CreatedAt
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> UnlinkExternalLogin(
        [FromQuery] string provider,
        [FromQuery] string providerUserId,
        IOAuthService oauthService,
        HttpContext httpContext)
    {
        var userId = TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
            // F-07：真实解绑链路——校验归属后删除 UserExternalLogin 绑定记录
            await oauthService.UnlinkExternalLoginFromUserAsync(userId.Value, provider, providerUserId);
            return Results.Ok(new { message = "External account unlinked successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Unlink Account",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
