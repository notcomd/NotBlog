using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Notcomd.Token.JWT;

public interface IJwtTokenService
{
    /// <summary>
    ///     构建token
    /// </summary>
    /// <typeparam name="T">需要声明Claim数据</typeparam>
    /// <param name="Redname"></param>
    /// <returns></returns>
    string BuilderTokenAsync(IEnumerable<Claim> claims, JwtOptions configuration);


    /// <summary>
    ///     解析jwt
    /// </summary>
    /// <param name="PrivateKey">用户密钥</param>
    /// <param name="AuthorizationString">生成的jwttoken</param>
    /// <returns></returns>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync([Required(ErrorMessage = "privatekey is null!")]string PrivateKey, string AuthorizationString);

    /// <summary>
    ///     解析token的重写方法
    /// </summary>
    /// <param name="authorizetionString">get/post/put/delete所有请求的请求报文头的token</param>
    /// <returns></returns>
    Task<TokenValidationResult> JwtSecurityTokenHandlerAsync(string authorizetionString);
}