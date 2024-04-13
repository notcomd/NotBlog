using System.Security.Claims;

namespace Notcomd.Token.JWT
{
    public interface IJWToken
    {
        /// <summary>
        /// 构建token 
        /// </summary>
        /// <typeparam name="T">需要声明Claim数据</typeparam>
        /// <param name="Redname"></param>
        /// <returns></returns>
        string BuilderTokenAsync(IEnumerable<Claim> claims, JwtokenModule jwtokenModule);
    }
}
