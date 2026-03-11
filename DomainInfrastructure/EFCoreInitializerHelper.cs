using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.DomainCommand;

public static class EFCoreInitializerHelper
{
    public static IServiceCollection AutoAddDbContextBuilder(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder> optionsBuilder, IEnumerable<Assembly> assemblies)
    {
        Type[] types =
        [
            typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(ServiceLifetime),
            typeof(ServiceLifetime)
        ];
        var dbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethod(nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext), 1, types);
        foreach (var item in assemblies)
        {
            var typesInAsm = item.GetTypes();
            foreach (var type in typesInAsm.Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t)))
            {
                var dbContextMethodAddDbContext = dbContextMethod?.MakeGenericMethod(type);
                dbContextMethodAddDbContext?.Invoke(null, new object[]
                {
                    serviceCollection, optionsBuilder, ServiceLifetime.Scoped, ServiceLifetime.Scoped
                });
            }
        }

        return serviceCollection;
    }

    public static IServiceCollection AutoAddDbContextBuilder(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsBuilder,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        foreach (var assembly in assemblies)
        {
            var dbContextTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t))
                .ToList();

            foreach (var type in dbContextTypes)
                try
                {
                    // 使用泛型方式注册 DbContext
                    var method = typeof(EntityFrameworkServiceCollectionExtensions)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext) &&
                            m.IsGenericMethod &&
                            m.GetParameters().Length == 4);

                    if (method == null) throw new InvalidOperationException("无法找到 AddDbContext 方法。");

                    var genericMethod = method.MakeGenericMethod(type);
                    genericMethod.Invoke(null, new object[]
                    {
                        services, optionsBuilder, contextLifetime, optionsLifetime
                    });

                    Console.WriteLine($"成功注册 DbContext: {type.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"注册 DbContext {type.FullName} 失败: {ex.InnerException?.Message}");
                }
        }

        return services;
    }


    public static IServiceCollection AddAllDbContexts(this IServiceCollection services,
        Action<DbContextOptionsBuilder> builder,
        IEnumerable<Assembly> assemblies)
    {
        //AddDbContextPool不支持DbContext注入其他对象，而且使用不当有内存暴涨的问题，因此不用AddDbContextPool
        var types = new[]
        {
            typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(ServiceLifetime),
            typeof(ServiceLifetime)
        };
        var methodAddDbContext = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethod(nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext), 1, types);
        foreach (var asmToLoad in assemblies)
        {
            var typesInAsm = asmToLoad.GetTypes();
            //Register DbContext
            //GetTypes() include public/protected ones
            //GetExportedTypes only include public ones
            //so that XXDbContext in Agrregation can be internal to keep insulated
            foreach (var dbCtxType in typesInAsm
                         .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t)))
            {
                //similar to serviceCollection.AddDbContextPool<ECDictDbContext>(opt=>new DbContextOptionsBuilder(dbCtxOpt));
                var methodGenericAddDbContext = methodAddDbContext.MakeGenericMethod(dbCtxType);
                methodGenericAddDbContext.Invoke(null, new object[]
                {
                    services, builder, ServiceLifetime.Scoped, ServiceLifetime.Scoped
                });
            }
        }

        return services;
    }
}