using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Message.Web.API.Hubs;

/// <summary>
/// 从 JWT Claim（sub / user_guid / NameIdentifier）解析用户 ID，供 SignalR Clients.User 定位连接。
/// SignalR 默认的 DefaultUserIdProvider 只读取 NameIdentifier，而本应用 JWT 主 Claim 为 sub（或 user_guid），
/// 若不覆盖则 Clients.User(...) 推送无法命中连接（F-04）。解析顺序与 MessageHub / CurrentUserService 保持一致。
/// </summary>
public class MessageUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => ResolveUserId(connection.User);

    /// <summary>
    /// 从 Claim 解析用户 ID（优先级：sub → NameIdentifier → user_guid），供测试与连接校验复用。
    /// </summary>
    public static string? ResolveUserId(ClaimsPrincipal? user)
        => user?.FindFirst("sub")?.Value
           ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? user?.FindFirst("user_guid")?.Value;
}
