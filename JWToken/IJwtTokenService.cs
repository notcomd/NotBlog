using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT;

public interface IJwtTokenService
{
    /// <summary>
    /// 构建token
    /// </summary>
    /// <param name="claims"> 用户声明 </param>
    /// <param name="configuration"> Jwt配置类 </param>
    /// <returns></returns>
    string BuilderTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration);


    /// <summary>
    /// 解析jwt
    /// </summary>
    /// <param name="privateKey">用户密钥</param>
    /// <param name="authorizationString">生成的token</param>
    /// <returns></returns>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(
        [Required(ErrorMessage = "privateKey is null!")] string privateKey, string authorizationString);

    /// <summary>
    /// 解析token的重写方法
    /// </summary>
    /// <returns></returns>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(string authorizationString);
}