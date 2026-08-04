using NotMediator;

namespace DomainCommons;

/// <summary>
/// 领域事件容器接口
/// <para>聚合根/实体实现此接口以支持领域事件的统一分发。</para>
/// <para>替代 <c>MediatorExtensions</c> 中基于字符串反射的查找方式，提供编译期类型安全。</para>
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>获取所有待发布的领域事件（只读视图）</summary>
    IReadOnlyCollection<INotifications> DomainEvents { get; }

    /// <summary>添加一个领域事件到待发布列表</summary>
    void AddDomainEvent(INotifications notification);

    /// <summary>清除所有待发布的领域事件（通常在事件分发完成后调用）</summary>
    void ClearDomainEvents();
}
