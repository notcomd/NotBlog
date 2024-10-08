using System.Text;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT
{
    public static class AuthenticationExtensions
    {

        //#
        //这是jwtoken配置类,加载配置信息
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection serviceDescriptors, JwtOptions wToke)
        {
            serviceDescriptors.AddScoped<INotcomd_JwtTokenServer, Notcommd_JWTokenOptions>();
            return serviceDescriptors.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new()
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
            services.AddScoped<INotcomd_JwtTokenServer, Notcommd_JWTokenOptions>();
            var ConfigString = configuration.Get<JwtOptions>();
            if (ConfigString is null)
            {
                throw new ArgumentNullException("没有配置相关数据,请检查配置文件问题");
            }
            return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ConfigString.Issuer,
                    ValidAudience = ConfigString.Audiencs,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigString.PrivateKey))
                };
            });
        }
    }
}
