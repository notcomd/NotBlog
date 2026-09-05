namespace Identity.Domain.IRepository;

/// <summary>
/// 管理端用户列表的安全投影（不含 PasswordHash / UserSafety / UserAccessFail 等敏感字段）。
/// </summary>
/// <param name="UserGuid">用户ID</param>
/// <param name="UserName">用户名（可能为邮箱）</param>
/// <param name="UserEmail">用户邮箱</param>
/// <param name="AvatarUrl">头像地址</param>
/// <param name="Phone">手机号（可能为空）</param>
/// <param name="CreateDatetime">注册时间</param>
/// <param name="IsLockedOut">是否处于封禁/锁定状态</param>
public record AdminUserBrief(
    Guid UserGuid,
    string? UserName,
    string UserEmail,
    string? AvatarUrl,
    string? Phone,
    DateTimeOffset CreateDatetime,
    bool IsLockedOut);