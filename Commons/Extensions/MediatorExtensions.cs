using Commons.Core;
using Microsoft.EntityFrameworkCore;
using NotMediator;

namespace Commons.Extensions;

/// <summary>
/// 领域事件分发扩展方法
/// 提供从 DbContext 的 ChangeTracker 中提取并分发领域事件的统一入口。
/// 所有 Infrastructure 项目通过此扩展方法替代各自独立的 NotMediatorExtension。
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// 从 DbContext 的 ChangeTracker 中提取所有领域事件，清空事件集合，并通过 INotMediator 分发。
    /// 应在 SaveChangesAsync 之前调用。
    /// </summary>
    /// <param name="mediator">消息中介（INotMediator 实例）</param>
    /// <param name="dbContext">当前 DbContext 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="parallel">是否并发发布事件。默认 false（顺序发布），设为 true 则并发发布并在所有任务完成后检查异常。</param>
    /// <exception cref="ArgumentNullException">当 mediator 或 dbContext 为 null 时抛出</exception>
    /// <exception cref="AggregateException">当 parallel=true 且有部分事件发布失败时抛出聚合异常</exception>
    public static async Task DispatchDomainEventsAsync(
        this INotMediator mediator,
        DbContext dbContext,
        CancellationToken cancellationToken = default,
        bool parallel = false)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

        // 获取所有被追踪且实现了 IHasDomainEvents 的实体，提取其领域事件
        var entitiesWithEvents = GetEntitiesWithDomainEvents(dbContext);

        if (entitiesWithEvents.Count == 0)
            return;

        // 收集所有待发布的事件（快照，防止 ClearDomainEvents 后丢失）
        var allEvents = new List<INotifications>();
        foreach (var (entity, events) in entitiesWithEvents)
        {
            allEvents.AddRange(events);
        }

        // 清空所有实体的领域事件集合
        foreach (var (entity, _) in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        // 分发事件
        if (parallel)
        {
            var publishTasks = allEvents
                .Select(evt => mediator.PublishAsync(evt, cancellationToken))
                .ToList();

            try
            {
                await Task.WhenAll(publishTasks);
            }
            catch (Exception)
            {
                throw new AggregateException(
                    "一个或多个领域事件发布失败。",
                    publishTasks.Where(t => t.IsFaulted).Select(t => t.Exception!.InnerException!));
            }
        }
        else
        {
            foreach (var domainEvent in allEvents)
            {
                await mediator.PublishAsync(domainEvent, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 扫描 ChangeTracker 中所有实现了 IHasDomainEvents 接口且包含待发布事件的实体，
    /// 返回实体引用与其事件快照的配对列表。
    /// </summary>
    private static List<(IHasDomainEvents Entity, IReadOnlyCollection<INotifications> Events)> GetEntitiesWithDomainEvents(
        DbContext dbContext)
    {
        var result = new List<(IHasDomainEvents, IReadOnlyCollection<INotifications>)>();

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached)
                continue;

            if (entry.Entity is not IHasDomainEvents hasEvents)
                continue;

            var events = hasEvents.DomainEvents;
            if (events.Count > 0)
            {
                result.Add((hasEvents, events));
            }
        }

        return result;
    }
}
