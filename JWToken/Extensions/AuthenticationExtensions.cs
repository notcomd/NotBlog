using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Notcomd.Token.JWT.Core;
using SecurityAlgorithms = Notcomd.Token.JWT.Core.SecurityAlgorithms;

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
                AttachBlacklistCheck(x);
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
    /// 注册 JWT 服务（不注册认证中间件，适用于微服务客户端场景）
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
    /// 注意：黑名单当前为进程内实现，多实例部署需替换为 Redis 共享存储后即可全局生效，
    /// JwtBearer 挂接逻辑无需改动。
    /// </summary>
    private static void AttachBlacklistCheck(JwtBearerOptions x)
    {
        x.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenService = context.HttpContext.RequestServices.GetService<IJwtTokenService>();
                if (tokenService is null)
                    return Task.CompletedTask;

                var rawToken = context.SecurityToken is JwtSecurityToken jwt
                    ? jwt.RawData
                    : context.Request.Headers.Authorization.ToString()
                        .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
                        .Trim();

                if (tokenService.IsRevoked(rawToken))
                    context.Fail("Token 已吊销，请重新登录");

                return Task.CompletedTask;
            }
        };
    }

    private static TokenValidationParameters BuildValidationParameters(JwtOptions options)
    {
        var audience = options.Audiences ?? options.Issuer;
        var algorithm = options.Algorithm switch
        {
            "HS384" => SecurityAlgorithms.HmacSha384,
            "HS512" => SecurityAlgorithms.HmacSha512,
            _ => SecurityAlgorithms.HmacSha256
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
            TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.PrivateKey)),
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds)
        };
    }
}
