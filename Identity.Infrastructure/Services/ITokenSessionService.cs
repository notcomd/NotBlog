namespace Identity.Infrastructure.Services;

/// <summary>
/// 用户 Token 会话登记服务（P3：多设备独立会话）
///
/// 背景：此前 access/refresh token 按用户单槽缓存（auth:token:{userGuid}），
/// 第二台设备登录覆盖第一台、改密清缓存即"全端下线"。修复为按 token 指纹隔离的会话条目：
///   auth:session:{fp}       — 单设备会话条目（JSON，TTL 随 token 有效期）
///   auth:user:{userGuid}    — 该用户全部会话指纹集合（Set）
/// 登出仅吊销当前设备会话；改密/禁用可吊销用户全部已登记会话。
/// 注：黑名单为进程内实现（JWToken），多实例部署时吊销跨实例生效有限——已登记会话的
/// 吊销按实例执行，未登记的历史 token 依赖 JWT 自然过期。
/// </summary>
public interface ITokenSessionService
{
    /// <summary>
    /// 登记新会话（每个 token 对独立槽位，多设备互不覆盖）
    /// </summary>
    Task RegisterAsync(Guid userGuid, TokenResult tokenResult, TimeSpan accessTtl, TimeSpan refreshTtl,
        CancellationToken ct = default);

    /// <summary>
    /// 按 access token 吊销当前设备会话（登出：仅当前设备下线）
    /// </summary>
    Task RevokeSessionAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// 吊销用户全部已登记会话（改密/禁用：全端下线）
    /// </summary>
    Task RevokeAllSessionsAsync(Guid userGuid, CancellationToken ct = default);
}
