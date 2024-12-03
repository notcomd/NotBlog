using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT
{
    public class JwtTokenOptions : IJwtTokenOptions
    {

        /// <summary>
        /// 通过直接读取配置文件密钥内容。
        /// </summary>
        private readonly IOptionsSnapshot<JwtOptions> _optionsSnapshot;

        public JwtTokenOptions(IOptionsSnapshot<JwtOptions> optionsSnapshot)
        {
            _optionsSnapshot = optionsSnapshot;
        }


        public string BuilderTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration)
        {
            //var expiry = TimeSpan.FromSeconds(configuration.ExpirSeconds);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.PrivateKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescript = new JwtSecurityToken(issuer: configuration.Issuer, audience: configuration.Audiencs, claims, DateTime.Now, DateTime.Now.AddDays(configuration.ExpirSeconds), signingCredentials: signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescript);
        }


        public async Task<TokenValidationResult> JwtSecurityTokenHandlerAsync([Required(ErrorMessage = "privatekey is null!")]string PrivateKey, string AuthorizationString)
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

        public async Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(string authorizetionString)
        {
            JwtSecurityTokenHandler tokenHeandder = new();
            TokenValidationParameters tokenValidation = new();
            var securikey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_optionsSnapshot.Value.PrivateKey));
            tokenValidation.IssuerSigningKey = securikey;
            tokenValidation.ValidateIssuer = true;
            tokenValidation.ValidateAudience = true;
            TokenValidationResult? claimsPrincipal = await tokenHeandder.ValidateTokenAsync(authorizetionString, tokenValidation);
            return claimsPrincipal;
        }
    }
}