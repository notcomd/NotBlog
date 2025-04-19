using System.Reflection;
using DomainCommonst;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection service, IEnumerable<Assembly> assemblies)
    {
        return service.AddMediatR(assemblies.ToArray());
    }

    public static async Task DispatchDomainEventsAsync(this IMediator mediator, DbContext dbContext)
    {
        var domainEntities = dbContext.ChangeTracker
            .Entries<IDomainEvents>()
            .Where(x => x.Entity.GetDomainEvents().Any());

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList(); //加ToList()是为立即加载，否则会延迟执行，到foreach的时候已经被ClearDomainEvents()了

        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);
        }
    }
}