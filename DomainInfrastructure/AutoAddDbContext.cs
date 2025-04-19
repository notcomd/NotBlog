using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public static class AutoAddDbContext
{
    public static IServiceCollection AutoAddDbContextBuilder(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder> optionsBuilder, IEnumerable<Assembly> assemblies)
    {
        Type[] types = [typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(ServiceLifetime), typeof(ServiceLifetime)];
        var dbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethod(nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext), 1, types);
        foreach (var item in assemblies)
        {
            Type[] typesInAsm = item.GetTypes();
            foreach (var type in typesInAsm.Where(t => t.IsAbstract && typeof(DbContext).IsAssignableFrom(t)))
            {
                var dbContextMethodAddDbContext = dbContextMethod.MakeGenericMethod(type);
                dbContextMethodAddDbContext.Invoke(null, new object[]
                {
                    serviceCollection, optionsBuilder, ServiceLifetime.Scoped, ServiceLifetime.Scoped
                });
            }
        }
        return serviceCollection;
    }
}