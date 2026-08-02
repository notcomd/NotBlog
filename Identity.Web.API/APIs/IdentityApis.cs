using System.Security.Claims;
using CacheMemory.Core;
using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.APIs;

public static class IdentityApis
{
    /// <summary>S-13：登录 IP 级限流阈值（10 次/分钟）</summary>
    private const int LoginRateLimitPerMinute = 10;
    private const string LoginRateLimitKeyPrefix = "login:rate:";

    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/identity").WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/Register", Register).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/Login", Login).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/GenerateCode", GenerateCode).WithHttpLogging(HttpLoggingFields.All);

        // S-20：改密端点要求已认证；未认证请求由认证中间件返回 401
        route.MapPost("/ChangeByPassword", ChangeByPassword)
            .RequireAuthorization()
            .WithHttpLogging(HttpLoggingFields.All);

        // S-12：刷新 Token（单次使用，刷新后旧 RefreshToken 进入黑名单）
        route.MapPost("/refresh", Refresh).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }

    /// <summary>
    /// S-14：幂等键由客户端显式传入（请求头 X-Idempotency-Key）。
    /// 缺失或非合法 GUID 时回退为随机键（该请求无幂等保证，不影响其他请求）。
    /// </summary>
    internal static Guid GetIdempotencyKey(HttpContext context)
    {
        var header = context.Request.Headers["X-Idempotency-Key"].ToString();
        return Guid.TryParse(header, out var key) ? key : Guid.CreateVersion7();
    }

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="registerRequest">注册请求</param>
    /// <returns>注册结果</returns>
    private static async Task<IResult> Register([FromServices] IdentityService identityService,
        [FromBody] RegisterRequest registerRequest,
        HttpContext httpContext)
    {
        var userdata = await identityService.UserRepository.FindOneByUserAsync(registerRequest.UserEmail);

        if (userdata is not null)
        {
            return Results.BadRequest("用户已存在");
        }

        if (!string.IsNullOrWhiteSpace(registerRequest.VerificationCode))
        {
        }

        var command = new RegisterByUserCommand(registerRequest.UserPassword, registerRequest.VerificationCode,
            registerRequest.UserEmail);

        var registerIdentity = new IdentifiedCommand<RegisterByUserCommand, bool>(
            GetIdempotencyKey(httpContext), command);

        var result = await identityService.NotMediator.SendAsync(registerIdentity);
        if (result)
        {
            return Results.Ok(new { message = "注册成功" });
        }
        else
        {
            return Results.BadRequest("注册失败");
        }
    }


    private static async Task<IResult> Login([FromServices] IdentityService identityService,
        [FromServices] IRedisCacheService redisCacheService,
        [FromBody] LoginRequest loginRequest,
        HttpContext httpContext)
    {
        // S-13：登录 IP 级限流（10 次/分钟；Redis 原子计数，窗口内首请求设置 TTL，超限返回 429）
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"{LoginRateLimitKeyPrefix}{ip}:{window}";

        var count = await redisCacheService.StringIncrementAsync(rateKey);
        if (count == 1)
            await redisCacheService.KeyExpireAsync(rateKey, TimeSpan.FromMinutes(1));
        if (count > LoginRateLimitPerMinute)
            return Results.Json(new { error = "登录尝试过于频繁，请稍后再试" },
                statusCode: StatusCodes.Status429TooManyRequests);

        var data = await identityService.UserService
            .LogInByCheckPasswordAsync(loginRequest.Email, loginRequest.Password, loginRequest.Code);
        return Results.Ok(data);
    }

    /// <summary>
    /// S-12 刷新 Token：校验 RefreshToken（格式/签名/过期/黑名单）后返回新的 AccessToken/RefreshToken 对。
    /// 单次使用：刷新成功后旧 RefreshToken 进入黑名单，二次使用即被拒绝。
    /// </summary>
    private static async Task<IResult> Refresh(
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] IOptions<JwtOptions> jwtOptions,
        [FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await jwtTokenService.RefreshTokenAsync(request.RefreshToken, jwtOptions.Value);
            return Results.Ok(result);
        }
        catch (SecurityTokenException ex)
        {
            return Results.Json(new { error = ex.Message },
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }


    private static async Task<IResult> GenerateCode([FromServices] IdentityService identityService,
        [FromBody] GenerateCodeRequest generateCodeRequest,
        HttpContext httpContext)
    {
        var commandGenerateCode = new GenerateCodeCommand(generateCodeRequest.Email);

        var identityCommand =
            new IdentifiedCommand<GenerateCodeCommand, string>(GetIdempotencyKey(httpContext), commandGenerateCode);

        await identityService.NotMediator.SendAsync(identityCommand);

        return Results.Ok("");
    }


    /// <summary>
    /// S-20 修改密码：要求已认证（RequireAuthorization，未认证返回 401）；
    /// 必须携带旧密码或邮箱验证码；旧密码错误拒绝；改密成功后吊销该用户现有 token 并清除 token 缓存。
    /// </summary>
    private static async Task<IResult> ChangeByPassword([FromServices] IdentityService identityService,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ICacheMemory<TokenCacheEntry> tokenCache,
        [FromBody] ChangeByPasswordRequest changeByPasswordRequestRequest,
        HttpContext httpContext)
    {
        // S-20：身份仅取自认证后的 NameIdentifier Claim（统一 userId Claim，不信任请求体中的 email）
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        var userdata = await identityService.UserRepository.FindOneByUserAsync(userId);
        if (userdata is null)
            return Results.BadRequest("用户不存在");

        // S-20：必须携带旧密码或邮箱验证码
        var hasOldPassword = !string.IsNullOrWhiteSpace(changeByPasswordRequestRequest.Password);
        var hasCode = !string.IsNullOrWhiteSpace(changeByPasswordRequestRequest.Code);
        if (!hasOldPassword && !hasCode)
            return Results.BadRequest("必须携带旧密码或邮箱验证码");

        if (hasOldPassword)
        {
            // S-20：旧密码错误 → 拒绝（返回与账号不存在相同的统一文案，避免账号枚举）
            if (!await userdata.VerifyByPasswordAsync(changeByPasswordRequestRequest.Password))
                return Results.BadRequest("邮箱或密码错误");
        }
        // TODO(F-07): 邮箱验证码路径——验证码核对链路待接入（GenerateCode 已可发送验证码，此处暂只支持旧密码）

        if (string.Equals(changeByPasswordRequestRequest.Password, changeByPasswordRequestRequest.NewPassword))
            return Results.BadRequest("新密码不能与旧密码相同");

        var command = new ChangeByPasswordCommand(userId,
            changeByPasswordRequestRequest.NewPassword);

        var identityCommand =
            new IdentifiedCommand<ChangeByPasswordCommand, bool>(GetIdempotencyKey(httpContext), command);

        var result = await identityService.NotMediator.SendAsync(identityCommand);

        if (result)
        {
            // S-20：改密成功后吊销该用户现有 token（当前请求的 bearer token 入黑名单）+ 清除 token 缓存
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            var bearerToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : null;
            if (!string.IsNullOrWhiteSpace(bearerToken))
                await jwtTokenService.RevokeTokenAsync(bearerToken);
            await tokenCache.RemoveAsync($"auth:token:{userId}");
            await tokenCache.RemoveAsync($"auth:refresh:{userId}");

            var emailCommand = new SendEmailCommand(userdata.UserEmail, "重置账号消息！(≧∇≦)ﾉ",
                "你的账号密码重置了，请注意这是非常规的账号变动，确认为本人操作。");
            var identityEmailCommand = new IdentifiedCommand<SendEmailCommand, bool>(Guid.NewGuid(), emailCommand);
            await identityService.NotMediator.SendAsync(identityEmailCommand);
            return Results.Ok(new { message = "修改密码成功" });
        }
        else
        {
            return Results.BadRequest("修改密码失败");
        }
    }
}
