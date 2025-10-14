using System.Reflection;

using DomainCommon;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NotMediator;
namespace Notcomd.DomainCommand;

public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection service, IEnumerable<Assembly> assemblies)
    {
        return service.AddNotMediator(assemblies.ToArray());
    }

    public async static Task DispatchDomainEventsAsync(this INotMediator mediator, DbContext dbContext)
    {
        var domainEntities = dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any() && x.Entity.DomainEvents is not null);

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList(); //加ToList()是为立即加载，否则会延迟执行，到foreach的时候已经被ClearDomainEvents()了

        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await mediator.PublishAsync(domainEvent);
        }
    }
}