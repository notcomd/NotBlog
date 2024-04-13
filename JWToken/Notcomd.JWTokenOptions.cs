using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Notcomd.Token.JWT
{
    public class JWTokenOptions : IJWToken
    {
        string IJWToken.BuilderTokenAsync(IEnumerable<Claim> claims, JwtokenModule jwtokenModule)
        {
            var Securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtokenModule.Key));
            _ = new SigningCredentials(Securitykey, SecurityAlgorithms.HmacSha256);   //加密
            var TokenDescript = new JwtSecurityToken(jwtokenModule.Selerboot, jwtokenModule.Rootboot, claims);
            return new JwtSecurityTokenHandler().WriteToken(TokenDescript);
        }
    }
}
