namespace Video.Domain.IServices;

/// <summary>
/// 当前登录用户服务 — 从 HttpContext 解析认证后的用户标识。
/// 所有写操作（上传/更新/删除/点赞/收藏）必须使用服务端解析的用户 id，
/// 禁止信任客户端传入的 UserGuid / AffiliatedAuthorizes。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前登录用户 Guid；未认证时为 Guid.Empty。</summary>
    Guid UserGuid { get; }

    /// <summary>是否已认证。</summary>
    bool IsAuthenticated { get; }
}
