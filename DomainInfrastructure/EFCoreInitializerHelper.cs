using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DomainInfrastructure;

/// <summary>
/// EF Core DbContext 自动注册扩展
/// 通过反射扫描程序集中所有 DbContext 子类并自动注册到 DI 容器。
/// 
/// 注意：此方法依赖反射查找 EntityFrameworkServiceCollectionExtensions.AddDbContext 方法，
/// EF Core 版本升级时若方法签名变化可能需要调整。
/// </summary>
public static class EFCoreInitializerHelper
{
    /// <summary>
    /// 自动扫描并注册所有非抽象 DbContext 子类到 DI 容器（所有 DbContext 共用同一配置委托）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="optionsBuilder">DbContext 配置委托（如设置连接字符串）</param>
    /// <param name="assemblies">要扫描的程序集集合</param>
    /// <param name="contextLifetime">DbContext 生命周期，默认 Scoped</param>
    /// <param name="optionsLifetime">DbContextOptions 生命周期，默认 Scoped</param>
    /// <exception cref="ArgumentNullException">当任一参数为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当无法找到 AddDbContext 方法时抛出</exception>
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

        return RegisterAllDbContexts(services, _ => optionsBuilder, assemblies, contextLifetime, optionsLifetime);
    }

    /// <summary>
    /// 自动扫描并注册所有非抽象 DbContext 子类到 DI 容器（按 DbContext 类型指定独立连接串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionStringSelector">按 DbContext 类型返回其连接字符串的委托</param>
    /// <param name="assemblies">要扫描的程序集集合</param>
    /// <param name="contextLifetime">DbContext 生命周期，默认 Scoped</param>
    /// <param name="optionsLifetime">DbContextOptions 生命周期，默认 Scoped</param>
    /// <exception cref="ArgumentNullException">当任一参数为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当无法找到 AddDbContext 方法时抛出</exception>
    public static IServiceCollection AddAllDbContexts(
        this IServiceCollection services,
        Func<Type, string> connectionStringSelector,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (connectionStringSelector == null) throw new ArgumentNullException(nameof(connectionStringSelector));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        // 为每个 DbContext 类型构造独立委托：ctx => ctx.UseNpgsql(connectionStringSelector(contextType))
        return RegisterAllDbContexts(
            services,
            contextType => ctx => ctx.UseNpgsql(connectionStringSelector(contextType)),
            assemblies,
            contextLifetime,
            optionsLifetime);
    }

    /// <summary>
    /// 扫描程序集并反射调用 AddDbContext 完成注册的核心逻辑。
    /// </summary>
    private static IServiceCollection RegisterAllDbContexts(
        IServiceCollection services,
        Func<Type, Action<DbContextOptionsBuilder>> optionsBuilderFactory,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime contextLifetime,
        ServiceLifetime optionsLifetime)
    {
        var addDbContextMethod = FindAddDbContextMethod();

        foreach (var assembly in assemblies)
        {
            var dbContextTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

            foreach (var dbContextType in dbContextTypes)
            {
                try
                {
                    var optionsBuilder = optionsBuilderFactory(dbContextType);
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
                    // 静态库无 logger 可用，用 Debug 输出避免正式环境噪音
                    System.Diagnostics.Debug.WriteLine(
                        $"注册 DbContext {dbContextType.FullName} 失败: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        return services;
    }

    /// <summary>
    /// 查找 EntityFrameworkServiceCollectionExtensions.AddDbContext 方法。
    /// 匹配条件：名为 AddDbContext 的泛型静态方法，第一个参数为 IServiceCollection 类型，
    /// 且参数数量 >= 3（最低要求：services、optionsAction、contextLifetime）。
    /// </summary>
    private static MethodInfo FindAddDbContextMethod()
    {
        var method = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                && m.IsGenericMethod
                && m.GetParameters().Length >= 3
                && m.GetParameters()[0].ParameterType == typeof(IServiceCollection));

        return method ?? throw new InvalidOperationException(
            "无法找到 AddDbContext 方法，请检查 EF Core 版本兼容性。");
    }
}
