namespace DomainCommons;

/// <summary>
/// 领域事件接口
/// 所有领域事件都应实现此接口
/// </summary>
public interface IDomainEvent
{
}

/// <summary>
/// 领域事件容器接口
/// 聚合根实现此接口以支持领域事件的发布和清除
/// </summary>
public interface IDomainEvents
{
    /// <summary>获取所有待发布的领域事件</summary>
    IEnumerable<IDomainEvent> GetDomainEvents();

    /// <summary>添加一个领域事件</summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// 如果已存在相同类型的事件则跳过，否则添加
    /// 避免在同一事务中修改多个对象时重复触发同类事件
    /// </summary>
    void AddDomainEventIfAbsent(IDomainEvent domainEvent);

    /// <summary>清除所有已发布的领域事件</summary>
    void ClearDomainEvents();
}