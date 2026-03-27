using System.Reflection;
using DomainCommons;
using Microsoft.Extensions.DependencyInjection;

namespace DomainInfrastructure;

/// <summary>
/// 模块自动初始化器
/// 扫描程序集中所有 IModuleInitializer 实现并自动执行初始化
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// 自动扫描程序集并执行所有 IModuleInitializer 实现
    /// </summary>
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