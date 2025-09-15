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
    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection serviceDescriptors, JwtConfigurationOptions wToke)
    {
        serviceDescriptors.Configure<JwtConfigurationOptions>(options =>
        {
            options.Audiencs = wToke.Audiencs;
            options.Issuer = wToke.Issuer;
            options.PrivateKey = wToke.PrivateKey;
            options.ExpirSeconds = wToke.ExpirSeconds;
        });
        serviceDescriptors.AddOptions<JwtConfigurationOptions>()
            .ValidateOnStart();
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

    public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection(nameof(JwtConfigurationOptions));
        if (configSection.Exists())
            throw new ArgumentNullException($"没有配置{nameof(JwtConfigurationOptions)}");
        // var configString = configuration.GetSection(nameof(JwtConfigurationOptions)).Get<JwtConfigurationOptions>();

        var jwtOptions = configuration.Get<JwtConfigurationOptions>();

        services.AddOptions<JwtConfigurationOptions>()
            .ValidateOnStart();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        if (jwtOptions is null)
        {
            throw new ArgumentNullException("没有配置相关数据,请检查配置文件问题");
        }
        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwtOptions(options, jwtOptions));
    }


    //public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, Func<JwtConfigurationOptions,JwtConfigurationOptions> jwtConfigurationOptions)
    //{
    //    var configSection =jwtConfigurationOptions();
    //    services.Configure(configSection);
    //    services.AddOptions<JwtConfigurationOptions>()
    //        .ValidateOnStart();
    //    services.AddScoped<IJwtTokenService, JwtTokenService>();


    //    return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //        .AddJwtBearer(options => ConfigureJwtOptions(options, jwtOptions));
    //}



    private static void ConfigureJwtOptions(JwtBearerOptions jwtBearerOptions, JwtConfigurationOptions options)
    {
        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audiencs,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.PrivateKey))
        };
    }



}