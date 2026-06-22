namespace Identity.Domain.Events;

/// <summary>
/// 用户手机号注册事件
/// 不携带密码哈希等敏感数据——Handler 通过 UserGuid 查询 User 聚合获取
/// </summary>
public record UserStartedByPhoneDomainEvent(
    Guid UserGuid,
    HashSet<Guid> UserRoleGuid,
    PhoneNumber PhoneNumber,
    HashSet<Guid>? AuthorGuids) : INotifications;