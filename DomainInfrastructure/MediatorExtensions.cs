using DomainCommons;
using Microsoft.EntityFrameworkCore;

namespace DomainInfrastructure;

/// <summary>
/// 领域事件分发扩展
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// 从 DbContext 中提取并分发所有领域事件
    /// 在 SaveChangesAsync 之前调用此方法
    /// </summary>
    /// <param name="dispatcher">领域事件分发器</param>
    /// <param name="dbContext">当前 DbContext</param>
    public static async Task DispatchDomainEventsAsync(
        this Func<IDomainEvent, ValueTask> dispatcher,
        DbContext dbContext)
    {
        var domainEntities = dbContext.ChangeTracker
            .Entries<IDomainEvents>()
            .Where(x => x.Entity.GetDomainEvents().Any());

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();

        // 立即加载到列表，避免延迟执行时已被 ClearDomainEvents 清空
        foreach (var entity in domainEntities.ToList())
            entity.Entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await dispatcher(domainEvent);
    }
}