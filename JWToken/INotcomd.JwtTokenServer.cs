using Microsoft.Extensions.Configuration;

using System.Security.Claims;

namespace Notcomd.Token.JWT
{
    public interface INotcomd_JwtTokenServer
    {
        /// <summary>
        /// 构建token 
        /// </summary>
        /// <typeparam name="T">需要声明Claim数据</typeparam>
        /// <param name="Redname"></param>
        /// <returns></returns>
        string BuilderTokenAsync(IEnumerable<Claim> claims, IConfiguration configuration);
    }
}
