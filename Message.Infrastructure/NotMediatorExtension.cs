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

        var domainEventEntries = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEventbus?.Any() == true)
            .Select(e => e.Entity)
            .ToList();

        if (!domainEventEntries.Any())
            return;

        var allEvents = domainEventEntries
            .SelectMany(e => e.DomainEventbus!.ToList())
            .ToList();

        foreach (var entity in domainEventEntries)
        {
            entity.ClearDomainEvents();
        }

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