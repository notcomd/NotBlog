namespace Message.Web.API.Dto.Call;

/// <summary>
/// TURN 限时凭证（用于浏览器 RTCPeerConnection ICE）。
/// 返回结构直接对齐 coturn REST/前端 <c>RTCIceServer</c> 期望格式：{ urls, username, credential }。
/// </summary>
public class TurnCredentialsDto
{
    /// <summary>TURN 服务器地址数组</summary>
    public string[] Urls { get; set; } = [];

    /// <summary>限时用户名（格式：{过期Unix秒戳}:{用户ID}）</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>HMAC-SHA1 派生的限时凭证（Base64）</summary>
    public string Credential { get; set; } = string.Empty;
}