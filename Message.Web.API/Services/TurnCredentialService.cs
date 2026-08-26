using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Message.Domain.Options;
using Message.Web.API.Dto.Call;

namespace Message.Web.API.Services;

/// <summary>
/// WebRTC TURN 限时凭证签发服务（coturn use-auth-secret 规范）。
/// <para>
/// 生成 <c>username = "{expiryUnix}:{userId}"</c> 与
/// <c>credential = Base64(HMAC-SHA1(SharedSecret, username))</c>，
/// 前端拿到后由 RTCPeerConnection 直接使用；coturn 会用同一共享密钥反向校验签名与有效期。
/// </para>
/// </summary>
public sealed class TurnCredentialService
{
    private readonly TurnServiceOptions _options;

    public TurnCredentialService(IOptions<TurnServiceOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 为指定用户签发一组限时 TURN 凭证。
    /// </summary>
    /// <param name="userId">当前登录用户 ID，作为凭证主体绑定到对应会话</param>
    /// <returns>符合前端 RTCIceServer 结构的凭证</returns>
    public TurnCredentialsDto Generate(Guid userId)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(_options.TtlSeconds).ToUnixTimeSeconds();
        var username = $"{expiresAt}:{userId}";

        string credential;
        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_options.SharedSecret)))
        {
            credential = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(username)));
        }

        return new TurnCredentialsDto
        {
            Urls = _options.Urls,
            Username = username,
            Credential = credential
        };
    }
}