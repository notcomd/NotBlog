using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Notcomd.Token.JWT
{
    public class Notcommd_JWTokenOptions : INotcomd_JwtTokenServer
    {
        public string BuilderTokenAsync(IEnumerable<Claim> claims, IConfiguration configuration)
        {
            var ConfigValue = configuration.Get<Notcomd_JwtToken_Configural>();
            var Securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigValue.PrivateKey));
            _ = new SigningCredentials(Securitykey, SecurityAlgorithms.HmacSha256);   //加密
            var TokenDescript = new JwtSecurityToken(ConfigValue.Selerboot, ConfigValue.Rootboot, claims);
            return new JwtSecurityTokenHandler().WriteToken(TokenDescript);
        }

   
    }
}
