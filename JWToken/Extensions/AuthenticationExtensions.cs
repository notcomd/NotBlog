using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Notcomd.Token.JWT.Core;

namespace Notcomd.Token.JWT.Extensions;

/// <summary>
/// JWT 认证 DI 注册扩展
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// 使用 JwtOptions 对象注册 JWT 认证
    /// </summary>
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
        JwtOptions options)
    {
        // 仅注入一次
        if (services.All(s => s.ServiceType != typeof(IJwtTokenService)))
            services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                x.TokenValidationParameters = BuildValidationParameters(options);
                // ⚠️ 勿在 AttachBlacklistCheck 之后整体覆盖 x.Events：OnTokenValidated 黑名单检查会丢失
                AttachBlacklistCheck(x, logAuthenticationFailed: true);
            });
    }

    /// <summary>
    /// 使用 IConfiguration 注册 JWT 认证（从 appsettings.json / 环境变量绑定）
    /// </summary>
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        // 仅注入一次
        if (services.All(s => s.ServiceType != typeof(IJwtTokenService)))
            services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 绑定配置（凭据外置：私钥优先取配置，其次取共享环境变量 JWT_PRIVATE_KEY，均缺失时报清晰错误）
        var options = new JwtOptions();
        configuration.Bind(options);
        options.PrivateKey = ResolvePrivateKey(options.PrivateKey);

        // 注册到 DI options 系统，确保 IOptionsSnapshot<JwtOptions> 可以解析（签发方也使用解析后的密钥）
        services.Configure<JwtOptions>(opt =>
        {
            configuration.Bind(opt);
            opt.PrivateKey = ResolvePrivateKey(opt.PrivateKey);
        });

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                x.TokenValidationParameters = BuildValidationParameters(options);
                AttachBlacklistCheck(x);
            });
    }

    /// <summary>
    /// 解析 JWT 签名密钥：配置 → 环境变量 JWT_PRIVATE_KEY → 抛出清晰错误。
    /// 全站各服务验证的是 Identity 签发的令牌，必须使用同一把验证密钥，
    /// 因此统一从共享环境变量 JWT_PRIVATE_KEY 读取（支持部署时按环境轮换）。
    /// </summary>
    private static string ResolvePrivateKey(string? configuredKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey))
            return configuredKey;

        var envKey = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey;

        throw new InvalidOperationException(
            "JWT 签名密钥未配置：请在环境变量 JWT_PRIVATE_KEY（或配置 JwtOptions:PrivateKey）中设置。");
    }

    /// <summary>
    /// 注册 JWT 服务（不注册认证中间件，适用于微服务客户端场景）JWT_PRIVATE_KEY（或配置
    /// </summary>
    public static IServiceCollection AddJwtTokenService(this IServiceCollection services)
    {
        services.Configure<JwtOptions>(_ => { });
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }

    /// <summary>
    /// 接入黑名单校验（S-12）：Token 被吊销（RevokeTokenAsync）或 RefreshToken 被使用后，
    /// 即使签名有效也会在 OnTokenValidated 阶段被拒绝。
    /// 合并进既有 x.Events（若宿主已设置）而非整体覆盖；宿主在 PostConfigure 中改动
    /// JwtBearerOptions.Events 时必须保留 OnTokenValidated（见 Message 的 Hub access_token 配置）。
    /// 注意：黑名单当前为进程内实现，多实例/跨服务部署需替换为 Redis 共享存储后即可全局生效。
    /// </summary>
    private static void AttachBlacklistCheck(JwtBearerOptions x, bool logAuthenticationFailed = false)
    {
        var events = x.Events ?? new JwtBearerEvents();

        // 保留宿主既有钩子（如 Message PostConfigure 的 OnMessageReceived）
        var previousValidated = events.OnTokenValidated;
        events.OnTokenValidated = async context =>
        {
            if (previousValidated is not null)
                await previousValidated(context);
            await CheckBlacklistAsync(context);
        };

        if (logAuthenticationFailed)
        {
            var previousFailed = events.OnAuthenticationFailed;
            events.OnAuthenticationFailed = async context =>
            {
                if (previousFailed is not null)
                    await previousFailed(context);
                Console.WriteLine($"JWT验证失败: {context.Exception.Message}");
            };
        }

        x.Events = events;
    }

    /// <summary>黑名单校验（吊销的 AccessToken / 已使用的 RefreshToken 即使签名有效也被拒绝）</summary>
    private static Task CheckBlacklistAsync(TokenValidatedContext context)
    {
        var tokenService = context.HttpContext.RequestServices.GetService<IJwtTokenService>();
        if (tokenService is null)
            return Task.CompletedTask;

        var rawToken = ResolveRawToken(context);
        if (tokenService.IsRevoked(rawToken))
            context.Fail("Token 已吊销，请重新登录");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 提取本次已通过签名校验的原始 token 字符串（供黑名单比对）。
    /// <para>
    /// ⚠️ 历史缺陷：原实现仅识别 <see cref="JwtSecurityToken"/>，而 .NET 8+ 的 JwtBearer
    /// 默认改用 JsonWebTokenHandler，<see cref="TokenValidatedContext.SecurityToken"/> 实际为
    /// <see cref="JsonWebToken"/>，于是只能回退读取 Authorization 头——SignalR 的
    /// WebSocket/ServerSentEvents 传输（浏览器无法设置请求头，token 仅在 query access_token 中）
    /// 会因此拿到空串，被 <c>IsRevoked("")</c> 判为「已吊销」而误拒（Hub 连接 401 invalid_token）。
    /// </para>
    /// 提取顺序：SecurityToken（兼容两种 token 类型）→ query access_token → Authorization 头。
    /// </summary>
    private static string ResolveRawToken(TokenValidatedContext context)
    {
        switch (context.SecurityToken)
        {
            case JwtSecurityToken jwtSecurityToken:
                return jwtSecurityToken.RawData;
            case JsonWebToken jsonWebToken:
                return jsonWebToken.EncodedToken;
        }

        // Hub 传输（WebSocket/ServerSentEvents）无法携带请求头，token 位于 query（见 Message 的 Hub access_token 配置）
        var queryToken = context.HttpContext.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryToken))
            return queryToken;

        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : string.Empty;
    }

    private static TokenValidationParameters BuildValidationParameters(JwtOptions options)
    {
        var audience = options.Audiences ?? options.Issuer;
        // ⚠️ HS384 是 HMAC-SHA384（对称），此前误映射为 EcdsaSha384（非对称）——
        // 未设 ValidAlgorithms 时被宽松放过未暴露；现修正并用 ValidAlgorithms 收紧
        // 为与签发侧完全一致的算法白名单（配置漂移立即显形，防算法混淆）。
        var algorithm = options.Algorithm switch
        {
            "HS384" => Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha384,
            "HS512" => Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512,
            _ => Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
        };

        return new TokenValidationParameters
        {
            ValidateIssuer = options.ValidateIssuer,
            ValidateAudience = options.ValidateAudience,
            ValidateLifetime = options.ValidateLifetime,
            ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
            ValidIssuer = options.Issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.PrivateKey)),
            ValidAlgorithms = [algorithm],
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds)
        };
    }
}
