using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using System.Text;

namespace Notcomd.Token.JWT
{
    public static class AuthenticationExtensions
    {

        //#
        //这是jwtoken配置类,加载配置信息
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection serviceDescriptors, JwtokenModule wToke)
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
    }
}
