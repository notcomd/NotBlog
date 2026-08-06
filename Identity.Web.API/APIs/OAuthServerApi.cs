using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using CacheMemory.Core;
using Microsoft.IdentityModel.Tokens;
using Identity.Domain.Entities.ClientAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.APIs;

/// <summary>
/// OAuth 2.0 授权服务器端点（RFC 6749 授权码模式 + RFC 7636 PKCE）
///
/// 基于 NotClient 客户端注册实体：
///   GET  /api/identity/oauth/authorize — 授权端点（需登录，签发一次性授权码）
///   POST /api/identity/oauth/token     — Token 端点（授权码换 Token / 刷新 / 客户端凭证）
///
/// 授权码为短期一次性凭证，存 Redis（TTL 10 分钟，消费即删）。
/// </summary>
public static class OAuthServerApi
{
    /// <summary>授权码缓存 key 前缀</summary>
    private const string OAuthCodeKeyPrefix = "oauth:code:";

    /// <summary>授权码有效期</summary>
    private static readonly TimeSpan OAuthCodeTtl = TimeSpan.FromMinutes(10);

    /// <summary>授权码长度（字符）</summary>
    private const int OAuthCodeLength = 32;

    public static RouteGroupBuilder MapOAuthServerApi(this RouteGroupBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/oauth").WithTags("OAuth Server (RFC 6749)");

        // 授权端点：要求用户已登录（与外部 OAuth 回调端点区分）
        group.MapGet("/authorize", Authorize)
            .RequireAuthorization()
            .WithName("OAuthAuthorize")
            .WithDescription("OAuth 2.0 授权端点（授权码模式，需登录；支持 PKCE）");

        // Token 端点：匿名（通过 client_id / client_secret / 授权码自证）
        group.MapPost("/token", Token)
            .WithName("OAuthToken")
            .WithDescription("OAuth 2.0 Token 端点（authorization_code / refresh_token / client_credentials）");

        return group;
    }

    // ═══════════════════════════════════════════════════════════
    //  授权端点
    // ═══════════════════════════════════════════════════════════

    private static async Task<IResult> Authorize(
        [FromServices] INotClientRepository clientRepository,
        [FromServices] IRedisCacheService redisCacheService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var query = httpContext.Request.Query;

        // ── 1. response_type 必须为 code ──
        var responseType = query["response_type"].ToString();
        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
            return OAuthError("unsupported_response_type", "仅支持 response_type=code", 400);

        // ── 2. 客户端与回调地址校验 ──
        var clientId = query["client_id"].ToString();
        var redirectUri = query["redirect_uri"].ToString();
        var state = query["state"].ToString();

        var client = string.IsNullOrWhiteSpace(clientId)
            ? null
            : await clientRepository.FindByClientIdAsync(clientId, ct);

        if (client is null)
            return OAuthError("unauthorized_client", "client_id 不存在", 400);
        if (client.Status != ClientStatus.Active)
            return OAuthError("unauthorized_client", "客户端已被禁用或吊销", 400);
        if (!client.ValidateRedirectUri(redirectUri))
            return OAuthError("invalid_request", "redirect_uri 不在客户端白名单内", 400);

        // ── 3. scope 校验（必须为 AllowedScopes 的子集；空视为默认）──
        var scope = query["scope"].ToString();
        var requestedScopes = string.IsNullOrWhiteSpace(scope)
            ? new List<string>()
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (requestedScopes.Any(s => !client.ValidateScope(s)))
        {
            return RedirectOrError(client, redirectUri, state,
                "invalid_scope", "请求的 scope 超出客户端允许范围");
        }

        // ── 4. PKCE（RFC 7636）：RequirePkce 客户端必须携带 code_challenge ──
        var codeChallenge = query["code_challenge"].ToString();
        var codeChallengeMethod = query["code_challenge_method"].ToString();

        if (string.IsNullOrEmpty(codeChallenge) && client.RequirePkce)
        {
            return RedirectOrError(client, redirectUri, state,
                "invalid_request", "该客户端要求 PKCE，必须携带 code_challenge");
        }

        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(codeChallengeMethod))
                codeChallengeMethod = "plain";
            if (codeChallengeMethod is not ("S256" or "plain"))
            {
                return RedirectOrError(client, redirectUri, state,
                    "invalid_request", "code_challenge_method 仅支持 S256 或 plain");
            }
        }

        // ── 5. 当前登录用户（NameIdentifier Claim 为统一用户 ID）──
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        // ── 6. 生成一次性授权码并写入 Redis ──
        var code = JwtRandom.GenerateRandomString(OAuthCodeLength);
        var authCodeInfo = new OAuthAuthorizationCodeInfo(
            clientId, userId, redirectUri,
            string.Join(' ', requestedScopes),
            string.IsNullOrEmpty(codeChallenge) ? null : codeChallenge,
            string.IsNullOrEmpty(codeChallengeMethod) ? null : codeChallengeMethod);

        await redisCacheService.StringSetAsync(
            $"{OAuthCodeKeyPrefix}{code}",
            JsonSerializer.Serialize(authCodeInfo),
            OAuthCodeTtl, ct);

        // ── 7. 响应：默认 302 跳回（带 code + state）；response_mode=json 返回 JSON ──
        var responseMode = query["response_mode"].ToString();
        if (string.Equals(responseMode, "json", StringComparison.OrdinalIgnoreCase) ||
            httpContext.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            return Results.Ok(new
            {
                code,
                state,
                redirectUri,
                expiresIn = (int)OAuthCodeTtl.TotalSeconds
            });
        }

        var redirectUrl = $"{redirectUri}{(redirectUri.Contains('?') ? "&" : "?")}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            redirectUrl += $"&state={Uri.EscapeDataString(state)}";

        return Results.Redirect(redirectUrl);
    }

    // ═══════════════════════════════════════════════════════════
    //  Token 端点
    // ═══════════════════════════════════════════════════════════

    private static async Task<IResult> Token(
        [FromServices] INotClientRepository clientRepository,
        [FromServices] IRedisCacheService redisCacheService,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUserRoleRepository userRoleRepository,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] IOptions<JwtOptions> jwtOptions,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var form = await httpContext.Request.ReadFormAsync(ct);
        var grantType = form["grant_type"].ToString();

        return grantType switch
        {
            "authorization_code" => await TokenByAuthorizationCodeAsync(
                form, clientRepository, redisCacheService, userRepository, userRoleRepository,
                jwtTokenService, jwtOptions.Value, httpContext, ct),
            "refresh_token" => await TokenByRefreshTokenAsync(
                form, clientRepository, jwtTokenService, jwtOptions.Value, ct),
            "client_credentials" => await TokenByClientCredentialsAsync(
                form, clientRepository, jwtTokenService, jwtOptions.Value, httpContext, ct),
            _ => OAuthError("unsupported_grant_type", $"不支持的 grant_type: {grantType}", 400)
        };
    }

    /// <summary>
    /// 授权码换 Token（RFC 6749 §4.1.3 + RFC 7636 PKCE 校验）
    /// </summary>
    private static async Task<IResult> TokenByAuthorizationCodeAsync(
        IFormCollection form,
        INotClientRepository clientRepository,
        IRedisCacheService redisCacheService,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IJwtTokenService jwtTokenService,
        JwtOptions jwtOptions,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var clientId = form["client_id"].ToString();
        var code = form["code"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var codeVerifier = form["code_verifier"].ToString();

        var client = await ResolveAndValidateClientAsync(clientRepository, clientId, form, httpContext, ct);
        if (client is null)
            return OAuthError("invalid_client", "客户端校验失败", 401);

        // ── 授权码校验：存在、未消费、归属正确 ──
        if (string.IsNullOrWhiteSpace(code))
            return OAuthError("invalid_request", "缺少 code 参数", 400);

        var codeKey = $"{OAuthCodeKeyPrefix}{code}";
        var cached = await redisCacheService.StringGetAsync(codeKey, ct);
        if (string.IsNullOrEmpty(cached))
            return OAuthError("invalid_grant", "授权码无效或已过期", 400);

        var authCode = JsonSerializer.Deserialize<OAuthAuthorizationCodeInfo>(cached);
        if (authCode is null)
            return OAuthError("invalid_grant", "授权码无效或已过期", 400);

        // 消费授权码（一次性；先取后删，与 OAuth state 处理一致）
        await redisCacheService.KeyDeleteAsync(codeKey, ct);

        if (!string.Equals(authCode.ClientId, client.ClientId, StringComparison.Ordinal))
            return OAuthError("invalid_grant", "授权码与 client_id 不匹配", 400);

        if (!string.Equals(authCode.RedirectUri, redirectUri, StringComparison.Ordinal))
            return OAuthError("invalid_grant", "redirect_uri 与授权请求不一致", 400);

        // ── PKCE 校验（RFC 7636 §4.6）：授权请求携带了 challenge 则必须验证 verifier ──
        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier))
                return OAuthError("invalid_grant", "缺少 code_verifier（PKCE 客户端）", 400);

            var valid = authCode.CodeChallengeMethod switch
            {
                "S256" => string.Equals(
                    Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier))),
                    authCode.CodeChallenge, StringComparison.Ordinal),
                _ => string.Equals(codeVerifier, authCode.CodeChallenge, StringComparison.Ordinal)
            };

            if (!valid)
                return OAuthError("invalid_grant", "PKCE code_verifier 校验失败", 400);
        }

        // ── 签发用户 Token ──
        var user = await userRepository.FindOneByUserAsync(authCode.UserId);
        if (user is null)
            return OAuthError("invalid_grant", "授权用户不存在", 400);

        var roles = await userRoleRepository.FindByUserRoleAsync(user.UserRoleGuid.ToHashSet()) ?? [];
        var roleNames = string.Join(',', roles.Select(r => r.RoleName));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserGuid.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.UserEmail),
            new(ClaimTypes.Email, user.UserEmail),
            new(ClaimTypes.Role, roleNames),
            new("client_id", client.ClientId),
            new("scope", authCode.Scope)
        };

        var tokenResult = await jwtTokenService.BuildTokenAsync(claims, jwtOptions);

        return Results.Ok(BuildTokenResponse(tokenResult, authCode.Scope));
    }

    /// <summary>
    /// 刷新 Token（复用 JWToken 的单次使用 + 黑名单语义）
    /// </summary>
    private static async Task<IResult> TokenByRefreshTokenAsync(
        IFormCollection form,
        INotClientRepository clientRepository,
        IJwtTokenService jwtTokenService,
        JwtOptions jwtOptions,
        CancellationToken ct)
    {
        var refreshToken = form["refresh_token"].ToString();
        if (string.IsNullOrWhiteSpace(refreshToken))
            return OAuthError("invalid_request", "缺少 refresh_token 参数", 400);

        // 提供了 client_id 时校验客户端有效性（不强制绑定，与现有 /refresh 行为一致）
        var clientId = form["client_id"].ToString();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var client = await clientRepository.FindByClientIdAsync(clientId, ct);
            if (client is null || client.Status != ClientStatus.Active)
                return OAuthError("invalid_client", "客户端不存在或不可用", 401);
        }

        try
        {
            var tokenResult = await jwtTokenService.RefreshTokenAsync(refreshToken, jwtOptions);
            return Results.Ok(BuildTokenResponse(tokenResult, null));
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return OAuthError("invalid_grant", $"刷新 Token 失败: {ex.Message}", 401);
        }
    }

    /// <summary>
    /// 客户端凭证模式（RFC 6749 §4.4，机器对机器）
    /// </summary>
    private static async Task<IResult> TokenByClientCredentialsAsync(
        IFormCollection form,
        INotClientRepository clientRepository,
        IJwtTokenService jwtTokenService,
        JwtOptions jwtOptions,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var clientId = form["client_id"].ToString();
        var client = await ResolveAndValidateClientAsync(clientRepository, clientId, form, httpContext, ct);
        if (client is null)
            return OAuthError("invalid_client", "客户端校验失败", 401);

        // scope 校验（可选，必须为允许范围的子集）
        var scope = form["scope"].ToString();
        var scopes = string.IsNullOrWhiteSpace(scope)
            ? new List<string>()
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (scopes.Any(s => !client.ValidateScope(s)))
            return OAuthError("invalid_scope", "请求的 scope 超出客户端允许范围", 400);

        var claims = new List<Claim>
        {
            new("client_id", client.ClientId),
            new("client_name", client.ApplicationName),
            new("scope", string.Join(' ', scopes))
        };

        var tokenResult = await jwtTokenService.BuildTokenAsync(claims, jwtOptions);

        // client_credentials 不返回 refresh_token（标准语义）
        return Results.Ok(BuildTokenResponse(tokenResult, string.Join(' ', scopes), includeRefresh: false));
    }

    // ═══════════════════════════════════════════════════════════
    //  私有帮助方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 解析并校验客户端（存在、Active、按 TokenEndpointAuthMethod 校验密钥）
    /// </summary>
    private static async Task<NotClient?> ResolveAndValidateClientAsync(
        INotClientRepository clientRepository,
        string clientId,
        IFormCollection form,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var client = await clientRepository.FindByClientIdAsync(clientId, ct);
        if (client is null || client.Status != ClientStatus.Active)
            return null;

        // 公共客户端 / 显式声明 none：无需密钥校验
        if (client.IsPublicClient ||
            string.Equals(client.TokenEndpointAuthMethod, "none", StringComparison.OrdinalIgnoreCase))
        {
            return client;
        }

        // 机密客户端：client_secret_basic（Authorization: Basic）或 client_secret_post（表单）
        var secret = form["client_secret"].ToString();

        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(authHeader["Basic ".Length..].Trim()));
                var colon = decoded.IndexOf(':');
                if (colon > 0)
                {
                    var headerClientId = decoded[..colon];
                    var headerSecret = decoded[(colon + 1)..];
                    if (string.Equals(headerClientId, client.ClientId, StringComparison.Ordinal))
                        secret = headerSecret;
                }
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return string.Equals(secret, client.ClientSecret, StringComparison.Ordinal) ? client : null;
    }

    /// <summary>构建 RFC 6749 §5.1 Token 响应</summary>
    private static object BuildTokenResponse(TokenResult tokenResult, string? scope, bool includeRefresh = true)
    {
        return new
        {
            access_token = tokenResult.AccessToken,
            token_type = string.IsNullOrEmpty(tokenResult.TokenType) ? "Bearer" : tokenResult.TokenType,
            expires_in = tokenResult.ExpiresAt != default
                ? (int)Math.Max(1, (tokenResult.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)
                : 3600,
            refresh_token = includeRefresh ? tokenResult.RefreshToken : null,
            scope = scope
        };
    }

    /// <summary>RFC 6749 §5.2 错误响应</summary>
    private static IResult OAuthError(string error, string description, int statusCode)
    {
        return Results.Json(
            new { error, error_description = description },
            statusCode: statusCode);
    }

    /// <summary>授权端点错误：redirect_uri 合法时跳回并携带 error 参数（RFC 6749 §4.1.2.1）</summary>
    private static IResult RedirectOrError(NotClient client, string redirectUri, string state,
        string error, string description)
    {
        if (client.ValidateRedirectUri(redirectUri))
        {
            var url = $"{redirectUri}{(redirectUri.Contains('?') ? "&" : "?")}error={Uri.EscapeDataString(error)}" +
                      $"&error_description={Uri.EscapeDataString(description)}";
            if (!string.IsNullOrEmpty(state))
                url += $"&state={Uri.EscapeDataString(state)}";
            return Results.Redirect(url);
        }

        return OAuthError(error, description, 400);
    }

    /// <summary>URL 安全 Base64 编码（RFC 7636 附录 B）</summary>
    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Redis 中缓存的授权码信息（消费即删，TTL 10 分钟）
/// </summary>
public sealed record OAuthAuthorizationCodeInfo(
    string ClientId,
    Guid UserId,
    string RedirectUri,
    string Scope,
    string? CodeChallenge,
    string? CodeChallengeMethod);
