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
            .AddJwtBearer(x => { x.TokenValidationParameters = BuildValidationParameters(options); });
    }

    /// <summary>
    /// 使用 IConfiguration 注册 JWT 认证（从 appsettings.json 绑定）
    /// </summary>
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        // 仅注入一次
        if (services.All(s => s.ServiceType != typeof(IJwtTokenService)))
            services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 绑定配置
        var options = new JwtOptions();
        configuration.Bind(options);

        if (string.IsNullOrEmpty(options.PrivateKey))
            throw new InvalidOperationException("JWT 签名密钥（PrivateKey）未配置");

        // 注册到 DI options 系统，确保 IOptionsSnapshot<JwtOptions> 可以解析
        services.Configure<JwtOptions>(configuration);

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x => { x.TokenValidationParameters = BuildValidationParameters(options); });
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