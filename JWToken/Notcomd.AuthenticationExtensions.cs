using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using static MongoDB.Driver.WriteConcern;

namespace Notcomd.Token.JWT
{
    public static class AuthenticationExtensions
    {

        //#
        //这是jwtoken配置类,加载配置信息
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection serviceDescriptors,Notcomd_JwtToken_Configural wToke)
        {
            return serviceDescriptors.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = wToke.Rootboot,
                    ValidAudience = wToke.Selerboot,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(wToke.Rootboot))
                };
            });
        }

        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var ConfigString = configuration.Get<Notcomd_JwtToken_Configural>();
            return services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme.ToString();
            }).AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ConfigString.Rootboot,
                    ValidAudience = ConfigString.Selerboot,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigString.Rootboot))
                };
            });
        }
    }
}
