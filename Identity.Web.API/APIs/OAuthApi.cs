using Identity.Domain.Dto.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Identity.Web.API.APIs;

public static class OAuthApi
{
    public static RouteGroupBuilder MapOAuthApi(this RouteGroupBuilder routeBuilder)
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

    private static IResult GetAvailableProviders([FromServices] IOptions<OAuthOptions> oauthOptions)
    {
        var options = oauthOptions.Value;
        // 仅返回「可用于登录/注册」的提供商：wechat 仅支持绑定后登录，不在此列出；
        // 其余提供商以 IsProviderAvailable 统一判定（Enabled 为真且关键凭据齐备）。
        var providers = new List<OAuthProviderInfo>
        {
            new("google", "Google", options.IsProviderAvailable("google")),
            new("github", "GitHub", options.IsProviderAvailable("github")),
            new("microsoft", "Microsoft", options.IsProviderAvailable("microsoft")),
            new("qq", "QQ", options.IsProviderAvailable("qq"))
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
        IOAuthService oauthService,
        IOutboxStore outboxStore,
        IdentityDbContext dbContext,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("OAuthApis");
        try
        {
            // S-11：state 必须透传，用于 CSRF 校验（GenerateAuthorizationUrlAsync 生成并存入 Redis）
            var response = await oauthService.HandleCallbackAsync(
                provider,
                request.Code,
                request.RedirectUri,
                request.State
            );
            
            if (response.IsNewUser)
            {
                await outboxStore.StoreAsync(new OutboxMessage(
                    nameof(RegisterByUserIntegrationEvent),
                    new RegisterByUserIntegrationEvent(response.UserInfo.UserId,
                        response.UserInfo.Email, response.UserInfo.UserName,
                        response.UserInfo.AvatarUrl)), ct);
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation(
                    "OAuth 新用户注册，已写入 Outbox：Provider={Provider}, UserId={UserId}",
                    provider, response.UserInfo.UserId);
            }

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
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
            logger.LogError(ex, "OAuth 回调处理失败：Provider={Provider}", provider);
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
        var userId = IdentityApiHelpers.TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
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
        var userId = IdentityApiHelpers.TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        
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
        var userId = IdentityApiHelpers.TryGetAuthenticatedUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        try
        {
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
