using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CacheMemory.Core;
using Identity.Web.API.Application.Commands;
using Identity.Web.API.Application.IntegrationEvents.Events;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notcomd.EventBus.Outbox;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.APIs;

public static class IdentityAuthApi
{

    private const int LoginRateLimitPerMinute = 10;
    private const string LoginRateLimitKeyPrefix = "login:rate:";

    public static RouteGroupBuilder MapIdentityAuthApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/identity").WithHttpLogging(HttpLoggingFields.All);

        // 统一登录/注册：邮箱 + 验证码（未注册邮箱自动注册并下发初始密码）
        route.MapPost("/Login", Login).WithHttpLogging(HttpLoggingFields.All);

        // 发码统一由 /email-verifications 提供（同一 GenerateCodeCommand），此前的 /GenerateCode 已移除

        // S-20：改密端点要求已认证；未认证请求由认证中间件返回 401
        route.MapPost("/ChangeByPassword", ChangeByPassword)
            .RequireAuthorization()
            .WithHttpLogging(HttpLoggingFields.All);

        // 更新当前登录用户的安全信息（如二次验证开关）：要求已认证
        route.MapPost("/UserSafety", UpdateUserSafety)
            .RequireAuthorization()
            .WithHttpLogging(HttpLoggingFields.All);

        // 读取当前登录用户的二次验证开关状态：要求已认证
        route.MapGet("/UserSafety", GetUserSafety)
            .RequireAuthorization()
            .WithHttpLogging(HttpLoggingFields.All);

        // S-12：刷新 Token（单次使用，刷新后旧 RefreshToken 进入黑名单）
        route.MapPost("/refresh", Refresh).WithHttpLogging(HttpLoggingFields.All);

        // P3：登出（吊销当前设备会话，不影响其他设备）
        route.MapPost("/Logout", Logout)
            .RequireAuthorization()
            .WithHttpLogging(HttpLoggingFields.All);

        return route;
    }

    /// <summary>
    /// 统一登录/注册接口：POST /identity/Login { email, password?, code? }。
    /// 携带密码 → 密码登入（开启二次验证的用户需同时携带 code）；
    /// 仅携带验证码 → 验证码登入，未注册邮箱自动注册（CQRS：LogInCommand + RegisterByEmailCommand）。
    /// 新注册用户在签发 Token 后发布 RegisterByUserIntegrationEvent（Outbox），通知下游服务初始化关联数据。
    /// </summary>
    private static async Task<IResult> Login([FromServices] IdentityServicesDi identityService,
        [FromServices] IRedisCacheService redisCacheService,
        [FromServices] IOutboxStore outboxStore,
        [FromServices] IdentityDbContext dbContext,
        [FromBody] LoginRequest loginRequest,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var ip = httpContext.GetClientIp();
        var window = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"{LoginRateLimitKeyPrefix}{ip}:{window}";

        var count = await redisCacheService.StringIncrementAsync(rateKey);
        if (count == 1)
            await redisCacheService.KeyExpireAsync(rateKey, TimeSpan.FromMinutes(1));
        if (count > LoginRateLimitPerMinute)
            return Results.Json(new { error = "登录尝试过于频繁，请稍后再试" },
                statusCode: StatusCodes.Status429TooManyRequests);


        if (string.IsNullOrWhiteSpace(loginRequest.Password) && string.IsNullOrWhiteSpace(loginRequest.Code))
            return Results.BadRequest(new { error = "请提供密码或邮箱验证码" });

        var command = new LogInCommand(loginRequest.Email, loginRequest.Password, loginRequest.Code);

        var data = await identityService.NotMediator.SendAsync(command);

        if (data is null || data.Token is null)
            return Results.Json(new { error = "邮箱、密码或验证码错误" },
                statusCode: StatusCodes.Status401Unauthorized);

        if (data.IsNewUser)
        {
            await outboxStore.StoreAsync(new OutboxMessage(
                nameof(RegisterByUserIntegrationEvent),
                new RegisterByUserIntegrationEvent(data.UserId, data.UserEmail ?? string.Empty,
                    data.UserName, data.AvatarUrl)), ct);
            await dbContext.SaveChangesAsync(ct);
        }

        
        return Results.Ok(new
        {
            data.Token!.AccessToken,
            data.Token.RefreshToken,
            data.Token.TokenType,
            data.Token.ExpiresAt,
            data.IsNewUser
        });
    }

    

    /// <summary>
    /// S-12 刷新 Token：校验 RefreshToken（格式/签名/过期/黑名单）后返回新的 AccessToken/RefreshToken 对。
    /// 单次使用：刷新成功后旧 RefreshToken 进入黑名单，二次使用即被拒绝。
    /// 刷新成功后把新 token 对登记进会话（P3 + 权限吊销盲区修复）——仅登录时登记的话，
    /// 刷新换发的 token 不在 auth:session 内，授权变更吊销（RevokeAllSessionsAsync）无法覆盖，
    /// 旧权限 claim 最长残留到 refresh 过期（7 天）。
    /// </summary>
    private static async Task<IResult> Refresh(
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ITokenSessionService tokenSessionService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] IOptions<JwtOptions> jwtOptions,
        [FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await jwtTokenService.RefreshTokenAsync(request.RefreshToken, jwtOptions.Value);
            await RegisterRefreshedSessionAsync(tokenSessionService, result, jwtOptions.Value,
                loggerFactory.CreateLogger("IdentityAuthApi.Refresh"));
            return Results.Ok(result);
        }
        catch (SecurityTokenException ex)
        {
            return Results.Json(new { error = ex.Message },
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// 登记刷新换发的新 token 对（与新登录同一会话体系；从新 access token 解析 userId——自签 token 可信）
    /// </summary>
    private static async Task RegisterRefreshedSessionAsync(
        ITokenSessionService tokenSessionService,
        TokenResult result,
        JwtOptions jwtOptions,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(result.AccessToken))
            return;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.AccessToken);
            var idClaim = jwt.Claims.FirstOrDefault(c =>
                c.Type is "nameid" or ClaimTypes.NameIdentifier or "sub" or "id");
            if (idClaim is null || !Guid.TryParse(idClaim.Value, out var userGuid))
                return;

            await tokenSessionService.RegisterAsync(userGuid, result,
                TimeSpan.FromSeconds(jwtOptions.ExpireSeconds),
                TimeSpan.FromSeconds(jwtOptions.RefreshTokenExpireSeconds));
        }
        catch (Exception ex)
        {
            // 登记失败不影响刷新结果（吊销覆盖降级为仅黑名单，与未登记的历史行为一致）
            logger.LogWarning(ex, "[Refresh] 刷新后会话登记失败，吊销覆盖不完整");
        }
    }


    /// <summary>
    /// P3：登出当前设备——吊销当前 Bearer Token（黑名单）+ 注销该设备会话登记；
    /// 仅影响当前设备，其他设备会话不受影响（全端下线请使用改密或后续的禁用端点）。
    /// </summary>
    private static async Task<IResult> Logout(
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ITokenSessionService tokenSessionService,
        HttpContext httpContext)
    {
        var bearerToken = IdentityApiHelpers.GetBearerToken(httpContext);
        if (string.IsNullOrWhiteSpace(bearerToken))
            return Results.BadRequest(new { error = "缺少 Bearer Token" });

        await jwtTokenService.RevokeTokenAsync(bearerToken);
        await tokenSessionService.RevokeSessionAsync(bearerToken);
        return Results.Ok(new { message = "已登出" });
    }


    /// <summary>
    /// S-20 修改密码：要求已认证（RequireAuthorization，未认证返回 401）；
    /// 必须携带旧密码或邮箱验证码；旧密码错误拒绝；改密成功后吊销该用户现有 token 并清除 token 缓存。
    /// </summary>
    private static async Task<IResult> ChangeByPassword([FromServices] IdentityServicesDi identityService,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ITokenSessionService tokenSessionService,
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
        // 邮箱验证码路径：交由 ChangeByPasswordCommandHandler 校验（修复此前 hasCode 路径绕过校验的漏洞）

        if (string.Equals(changeByPasswordRequestRequest.Password, changeByPasswordRequestRequest.NewPassword))
            return Results.BadRequest("新密码不能与旧密码相同");

        var command = new ChangeByPasswordCommand(userId,
            changeByPasswordRequestRequest.NewPassword,
            hasCode ? changeByPasswordRequestRequest.Code : null);

        var identityCommand =
            new IdentifiedCommand<ChangeByPasswordCommand, bool>(IdentityApiHelpers.GetIdempotencyKey(httpContext), command);

        var result = await identityService.NotMediator.SendAsync(identityCommand);

        if (result)
        {
            // S-20：改密成功后吊销该用户现有 token（当前请求的 bearer token 入黑名单）+ 清除 token 缓存
            var bearerToken = IdentityApiHelpers.GetBearerToken(httpContext);
            if (!string.IsNullOrWhiteSpace(bearerToken))
                await jwtTokenService.RevokeTokenAsync(bearerToken);
            // P3：吊销该用户全部已登记会话（多设备全端下线，替代此前仅清单槽缓存）
            await tokenSessionService.RevokeAllSessionsAsync(userId);

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


    /// <summary>
    /// 更新当前登录用户的安全信息（如二次验证开关）。
    /// 要求已认证（RequireAuthorization，未认证返回 401）；身份仅取自认证后的 NameIdentifier Claim。
    /// 关闭二次验证（置 false）为降级操作，必须携带密码或邮箱验证码二次确认。
    /// </summary>
    private static async Task<IResult> UpdateUserSafety([FromServices] IdentityServicesDi identityService,
        [FromBody] UpdateUserSafetyRequest request,
        HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        if (request.IsTwoFactorEnabled is null)
            return Results.BadRequest(new { error = "没有需要更新的安全信息" });

        var disabling = !request.IsTwoFactorEnabled.Value;
        if (disabling && string.IsNullOrWhiteSpace(request.Password) && string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new { error = "关闭二次验证需提供密码或邮箱验证码进行二次确认" });

        var ok = await identityService.UserService.UpdateUserSafetyAsync(
            userId, request.IsTwoFactorEnabled, request.Password, request.Code);
        if (!ok)
            return Results.BadRequest(new
            {
                error = disabling ? "二次确认失败，密码或验证码不正确" : "用户不存在或更新失败"
            });

        return Results.Ok(new
        {
            message = disabling ? "二次验证已关闭" : "二次验证已开启",
            IsTwoFactorEnabled = request.IsTwoFactorEnabled
        });
    }

    /// <summary>
    /// 读取当前登录用户的二次验证开关状态。要求已认证。
    /// </summary>
    private static async Task<IResult> GetUserSafety([FromServices] IdentityServicesDi identityService,
        HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        var isTwoFactorEnabled = await identityService.UserService.GetUserSafetyAsync(userId);
        if (isTwoFactorEnabled is null)
            return Results.BadRequest(new { error = "用户不存在" });

        return Results.Ok(new { IsTwoFactorEnabled = isTwoFactorEnabled });
    }
}
