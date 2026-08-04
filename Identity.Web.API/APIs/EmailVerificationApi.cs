using System.Net.Mail;
using CacheMemory.Core;
using Identity.Domain.ICache;
using Identity.Infrastructure.Idempotent;
using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// 邮件验证码 RESTful 端点。
/// 挂载于网关公共路径 /api/identity/ready 下（网关 PublicPaths 放行，无需登录即可发送/确认验证码）。
/// </summary>
public static class EmailVerificationApi
{
    /// <summary>S-13：发送验证码 IP 级限流阈值（3 次/分钟）</summary>
    private const int CodeRateLimitPerMinute = 3;
    private const string CodeRateLimitKeyPrefix = "code:rate:";

    /// <summary>确认端点 IP 级限流阈值（10 次/分钟，防止暴力破解验证码）</summary>
    private const int ConfirmRateLimitPerMinute = 10;
    private const string ConfirmRateLimitKeyPrefix = "code:confirm:rate:";

    /// <summary>验证码最大错误尝试次数（超过即失效，防止暴力穷举）</summary>
    private const int MaxCodeVerifyAttempts = 5;
    private const string CodeFailCountKeyPrefix = "code:fail:";

    public static RouteGroupBuilder MapEmailVerificationApi(this RouteGroupBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/email-verifications").WithTags("Email Verification");

        // 发送邮箱验证码
        group.MapPost("", SendCode);

        // 确认邮箱验证码结果
        group.MapPost("/confirm", ConfirmCode);

        return routeBuilder;
    }

    /// <summary>
    /// 发送邮箱验证码：校验邮箱格式 + IP 限流后调用命令发送。
    /// 返回 202 Accepted，且不回传验证码本身（S-16：验证码不得出现在响应与日志中）。
    /// </summary>
    private static async Task<IResult> SendCode(
        [FromServices] IdentityService identityService,
        [FromServices] IRedisCacheService redisCacheService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] GenerateCodeRequest request,
        HttpContext httpContext)
    {
        var logger = loggerFactory.CreateLogger("EmailVerificationApi");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "邮箱不能为空" });
        if (!MailAddress.TryCreate(request.Email, out _))
            return Results.BadRequest(new { error = "邮箱格式不正确" });

        // S-13：IP 级限流（3 次/分钟，Redis 原子计数，窗口内首请求设置 TTL）
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"{CodeRateLimitKeyPrefix}{ip}:{window}";
        var count = await redisCacheService.StringIncrementAsync(rateKey);
        if (count == 1)
            await redisCacheService.KeyExpireAsync(rateKey, TimeSpan.FromMinutes(1));
        if (count > CodeRateLimitPerMinute)
            return Results.Json(new { error = "发送过于频繁，请稍后再试" },
                statusCode: StatusCodes.Status429TooManyRequests);

        var command = new GenerateCodeCommand(request.Email);
        var identifiedCommand = new IdentifiedCommand<GenerateCodeCommand, string>(
            IdentityApis.GetIdempotencyKey(httpContext), command);

        try
        {
            await identityService.NotMediator.SendAsync(identifiedCommand);
            logger.LogInformation("验证码已发送：{Email}", request.Email);
            return Results.Accepted(null, new { message = "验证码已发送，请查收邮件" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送验证码失败：{Email}", request.Email);
            return Results.Problem(title: "发送验证码失败",
                detail: "邮件服务暂时不可用，请稍后再试",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// 确认邮箱验证码：校验缓存中验证码是否匹配（只校验不消费，注册流程在真正注册时才消费验证码）。
    /// 加 IP 级限流（10 次/分钟）+ 错误 5 次后验证码失效，防止暴力破解。
    /// </summary>
    private static async Task<IResult> ConfirmCode(
        [FromServices] IIdentityCacheService identityCacheService,
        [FromServices] IRedisCacheService redisCacheService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] ConfirmEmailCodeRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("EmailVerificationApi");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new { error = "邮箱和验证码不能为空" });

        // IP 级限流：10 次/分钟，防止暴力穷举验证码
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var rateKey = $"{ConfirmRateLimitKeyPrefix}{ip}:{window}";
        var count = await redisCacheService.StringIncrementAsync(rateKey);
        if (count == 1)
            await redisCacheService.KeyExpireAsync(rateKey, TimeSpan.FromMinutes(1));
        if (count > ConfirmRateLimitPerMinute)
            return Results.Json(new { error = "验证过于频繁，请稍后再试" },
                statusCode: StatusCodes.Status429TooManyRequests);

        var codeKey = $"Login_{request.Email}";
        var cached = await identityCacheService.GetStringAsync(codeKey, cancellationToken);
        var valid = cached is not null && string.Equals(cached, request.Code, StringComparison.Ordinal);

        if (!valid)
        {
            // 失败计数：超过 MaxCodeVerifyAttempts 次后删除验证码（失效），防止持续穷举
            var failKey = $"{CodeFailCountKeyPrefix}{request.Email}";
            var fails = await redisCacheService.StringIncrementAsync(failKey);
            if (fails == 1)
                await redisCacheService.KeyExpireAsync(failKey, TimeSpan.FromMinutes(5));
            if (fails >= MaxCodeVerifyAttempts)
            {
                await identityCacheService.RemoveAsync(codeKey, cancellationToken);
                await redisCacheService.KeyDeleteAsync(failKey);
            }
        }
        else
        {
            // 验证成功后清除失败计数
            await redisCacheService.KeyDeleteAsync($"{CodeFailCountKeyPrefix}{request.Email}");
        }

        // S-16：验证码本身不写日志，仅记录校验结果
        logger.LogInformation("验证码确认：{Email} => {Result}", request.Email, valid ? "通过" : "失败");
        return Results.Ok(new { valid });
    }
}
