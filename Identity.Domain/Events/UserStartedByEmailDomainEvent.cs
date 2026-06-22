namespace Identity.Domain.Events;

/// <summary>
/// 用户邮箱注册完成领域事件
/// 
/// 当用户通过邮箱成功注册后发布。
/// 携带注册基础信息供后续流程使用（如发送欢迎邮件、初始化默认配置）。
/// 
/// 注意：不携带密码哈希等敏感数据，事件处理方如需安全信息应通过查询获取。
/// </summary>
public record UserStartedByEmailDomainEvent(
    /// <summary>新注册用户的 ID</summary>
    Guid UserGuid,
    /// <summary>注册邮箱</summary>
    string UserEmail,
    /// <summary>分配的角色 ID</summary>
    Guid RoleGuid,
    /// <summary>关联的作者 ID 集合（可选，用于 OAuth 等第三方登录场景）</summary>
    HashSet<Guid>? AuthorGuids
) : INotifications;