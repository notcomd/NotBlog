namespace Message.Domain.Options;

/// <summary>
/// WebRTC TURN 限时凭证（coturn use-auth-secret 规范）配置项。
/// </summary>
public class TurnServiceOptions
{
    /// <summary>配置文件节名称（{SectionName}，即 appsettings 中的 "TurnService"）</summary>
    public const string SectionName = "TurnService";

    /// <summary>
    /// coturn 静态鉴权密钥（use-auth-secret）。
    /// 服务端用它与 <c>{expiry}:{username}</c> 做 HMAC-SHA1，得到一次性 TURN 凭证。
    /// ⚠️ 属敏感配置：仅从环境变量注入，勿提交到仓库/前端。
    /// </summary>
    public string SharedSecret { get; set; } = string.Empty;

    /// <summary>对外暴露的 TURN 服务器地址数组（如 "turn:turn.example.com:3478?transport=udp"）</summary>
    public string[] Urls { get; set; } = [];

    /// <summary>凭证有效期（秒），默认 1 小时</summary>
    public int TtlSeconds { get; set; } = 3600;
}