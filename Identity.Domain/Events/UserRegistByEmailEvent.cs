namespace Identity.Domain.Events;

/// <summary>
/// 用户邮箱注册事件
/// </summary>
public record UserRegistByEmailDomainEvent(
    /// <summary>新注册用户的 ID</summary>
    Guid UserGuid,
    /// <summary>注册邮箱</summary>
    string UserEmail,
    /// <summary>分配的角色 ID</summary>
    Guid RoleGuid,
    /// <summary>关联的作者 ID 集合（可选，用于 OAuth 等第三方登录场景）</summary>
    HashSet<Guid>? AuthorGuids
) : INotifications;