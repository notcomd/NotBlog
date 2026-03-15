using Message.Domain.SeedWork;
using Message.Infrastructure.EntityFramework;
using NotMediator;

namespace Message.Infrastructure;

public static class NotMediatorExtension
{
    public static async Task DispatchDomainEventsAsync(
        this INotMediator mediator,
        MessageDbContext context,
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
            // 并发发布所有事件
            var publishTasks = allEvents
                .Select(evt => mediator.PublishAsync(evt, cancellationToken))
                .ToList();

            try
            {
                await Task.WhenAll(publishTasks);
            }
            catch (Exception ex)
            {
                // 记录日志，抛出聚合异常或根据需要处理
                // 这里简单抛出，实际应考虑部分失败处理策略
                throw new AggregateException(
                    "One or more domain events failed to publish.",
                    publishTasks.Where(t => t.IsFaulted).Select(t => t.Exception!.InnerException!)
                );
            }
        }
        else
        {
            // 顺序发布
            foreach (var domainEvent in allEvents)
            {
                await mediator.PublishAsync(domainEvent, cancellationToken);
            }
        }
    }
}