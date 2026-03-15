using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT;

public static class AuthenticationExtensions
{
    //#
    //这是jwtoken配置类,加载配置信息
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection serviceDescriptors,
        JwtOptions wToke)
    {
        serviceDescriptors.AddScoped<IJwtTokenService, JwtTokenService>();
        return serviceDescriptors.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = wToke.Issuer,
                ValidAudience = wToke.Audiencs,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(wToke.PrivateKey))
            };
        });
    }

    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        var configString = configuration.Get<JwtOptions>();
        if (configString is null) throw new ArgumentNullException(nameof(configuration));
        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configString.Issuer,
                    ValidAudience = configString.Audiencs,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configString.PrivateKey))
                };
            });
    }
}