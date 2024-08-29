using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT
{
    public class Notcommd_JWTokenOptions : INotcomd_JwtTokenServer
    {

        public string BuilderTokenAsync(IEnumerable<Claim> claims, Notcomd_JwtOptions configuration)
        {
            //var expiry = TimeSpan.FromSeconds(configuration.ExpirSeconds);
            var Securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.PrivateKey));
            var signingCredentials = new SigningCredentials(Securitykey, SecurityAlgorithms.HmacSha256Signature);
            var TokenDescript = new JwtSecurityToken(issuer: configuration.Issuer, audience: configuration.Audiencs, claims,DateTime.Now, DateTime.Now.AddDays(configuration.ExpirSeconds), signingCredentials: signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(TokenDescript);
        }


        public async Task<TokenValidationResult> JwtSecurityTokenHandlerAsync([Required(ErrorMessage = "privatekey is null!")] string PrivateKey, string AuthorizationString)
        {
            JwtSecurityTokenHandler tokenHeandder = new();
            TokenValidationParameters tokenValidation = new();
            var securikey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PrivateKey));
            tokenValidation.IssuerSigningKey = securikey;
            tokenValidation.ValidateIssuer = true;
            tokenValidation.ValidateAudience = true;
            TokenValidationResult? claimsPrincipal = await tokenHeandder.ValidateTokenAsync(AuthorizationString, tokenValidation);
            return claimsPrincipal;
        }


    }
}
