using System.Reflection;
using Commons.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Commons.Extensions;

/// <summary>
/// 模块自动初始化器
/// 扫描指定程序集中所有 <see cref="IModuleInitializer"/> 的实现类，
/// 并自动创建实例执行初始化，完成服务注册。
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// 自动扫描程序集并执行所有 IModuleInitializer 实现。
    /// 对每个非抽象的实现类，通过无参构造函数创建实例并调用 Initialize 方法。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合（支持链式调用）</returns>
    /// <exception cref="InvalidOperationException">
    /// 当 IModuleInitializer 实现类没有无参构造函数或无法实例化时抛出
    /// </exception>
    public static IServiceCollection AddAutoAddInstance(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var initializerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IModuleInitializer).IsAssignableFrom(t));

            foreach (var type in initializerTypes)
            {
                var initializer = (IModuleInitializer?)Activator.CreateInstance(type);
                if (initializer == null)
                    throw new InvalidOperationException($"无法创建 {type.Name} 实例，请确保有无参构造函数。");

                initializer.Initialize(services);
            }
        }

        return services;
    }
}
