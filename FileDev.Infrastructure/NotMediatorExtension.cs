using FileDev.Domain.SeedWork;
using FileDev.Infrastructure.EntityFramework;
using NotMediator;

namespace ConsoleApp1;

public static class NotMediatorExtension
{
    public static async Task DispatchDomainEventsAsync(this INotMediator mediator, NotFileDbContext context)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (context == null) throw new ArgumentNullException(nameof(context));
        var domainEntities = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEventbus.Any())
            .Select(e => e.Entity);
        foreach (var entity in domainEntities)
        {
            foreach (var domainEvent in entity.DomainEventbus) await mediator.PublishAsync(domainEvent);
            entity.ClearDomainEvents();
        }
    }
}