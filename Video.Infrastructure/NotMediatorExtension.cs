using Microsoft.EntityFrameworkCore;
using NotMediator;
using Video.Domain.SeedWork;
using Video.Infrastructure.EntityFramework;

namespace Video.Infrastructure;

public static class NotMediatorExtension
{
    /// <summary>
    /// 分发领域事件，支持并发发布和异常隔离
    /// </summary>
    /// <param name="mediator">消息中介</param>
    /// <param name="context">DbContext 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="parallel">是否并发发布（默认 false 顺序发布）</param>
    public static async Task DispatchDomainEventsAsync(
        this INotMediator mediator,
        VideoDbContext context,
        CancellationToken cancellationToken = default,
        bool parallel = false)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (context == null) throw new ArgumentNullException(nameof(context));

        // 获取所有包含领域事件的实体，并提取事件快照，避免后续修改干扰
        var domainEventEntries = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEventbus?.Any() == true)
            .Select(e => e.Entity)
            .ToList();

        if (!domainEventEntries.Any())
            return;

        // 收集所有待发布的事件
        var allEvents = domainEventEntries
            .SelectMany(e => e.DomainEventbus!.ToList()) // 假设 DomainEventbus 不为 null
            .ToList();

        // 清空所有实体的领域事件集合（快照已保存，可安全清除）
        foreach (var entity in domainEventEntries)
        {
            entity.ClearDomainEvents();
        }

        // 发布事件（根据 parallel 参数选择策略）
        if (parallel)
        {
            var publishTasks = allEvents
                .Select(evt => mediator.PublishAsync(evt, cancellationToken))
                .ToList();
            try
            {
                await Task.WhenAll(publishTasks);
            }
            catch (Exception ex)
            {
                throw new AggregateException(
                    "One or more domain events failed to publish.",
                    publishTasks.Where(t => t.IsFaulted).Select(t => t.Exception!.InnerException!)
                );
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
}