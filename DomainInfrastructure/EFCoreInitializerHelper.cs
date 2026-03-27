using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DomainInfrastructure;

/// <summary>
/// EF Core DbContext 自动注册扩展
/// 扫描程序集中所有 DbContext 子类并自动注册到 DI 容器
/// </summary>
public static class EFCoreInitializerHelper
{
    /// <summary>
    /// 自动扫描并注册所有 DbContext
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="optionsBuilder">DbContext 配置（如连接字符串）</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="contextLifetime">DbContext 生命周期，默认 Scoped</param>
    /// <param name="optionsLifetime">Options 生命周期，默认 Scoped</param>
    public static IServiceCollection AddAllDbContexts(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsBuilder,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        var addDbContextMethod = FindAddDbContextMethod();

        foreach (var assembly in assemblies)
        {
            var dbContextTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

            foreach (var dbContextType in dbContextTypes)
            {
                try
                {
                    var genericMethod = addDbContextMethod.MakeGenericMethod(dbContextType);
                    genericMethod.Invoke(null, new object[]
                    {
                        services,
                        optionsBuilder,
                        contextLifetime,
                        optionsLifetime
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"注册 DbContext {dbContextType.FullName} 失败: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        return services;
    }

    private static MethodInfo FindAddDbContextMethod()
    {
        var method = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                && m.IsGenericMethod
                && m.GetParameters().Length == 4);

        return method ?? throw new InvalidOperationException("无法找到 AddDbContext 方法。");
    }
}