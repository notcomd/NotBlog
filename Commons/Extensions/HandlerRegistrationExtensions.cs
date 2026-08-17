using Microsoft.Extensions.DependencyInjection;

namespace Commons.Extensions;

/// <summary>
/// DI 注册清理扩展：移除 NotMediator 自动注册产生的「不可实例化」handler 条目。
/// </summary>
public static class HandlerRegistrationExtensions
{
    /// <summary>
    /// 移除抽象泛型基类 handler 的注册。
    /// NotMediator.AddNotMediator 扫描程序集时未过滤抽象类，会把抽象泛型基类
    /// （如 <see cref="NotMediator.Abstractions.IRequestHandler{TRequest,TResponse}"/> 的
    /// 抽象泛型实现 IdentifiedCommandHandler&lt;T,R&gt;）以「含泛型参数的开放接口类型」注册到
    /// 抽象实现类型上——DI 构建时抛 "Cannot instantiate implementation type ..."（2026-08-17
    /// Identity/FileDev 启动崩溃实锤）。具体闭合 handler 的注册（serviceType 不含泛型参数）
    /// 与合法的开放泛型注册（serviceType 为泛型类型定义，ContainsGenericParameters=false）
    /// 均不受影响。
    /// 应在 <c>AddNotMediator(...)</c> 之后调用。
    /// </summary>
    public static IServiceCollection RemoveAbstractHandlerRegistrations(this IServiceCollection services)
    {
        var toRemove = services
            .Where(sd => sd.ServiceType.IsGenericType
                && sd.ServiceType.ContainsGenericParameters
                && sd.ImplementationType is { IsAbstract: true })
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);

        return services;
    }
}
