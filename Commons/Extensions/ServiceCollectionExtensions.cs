using System.Reflection;
using Commons.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commons.Extensions;

/// <summary>
/// 服务注册扩展
/// 统一配置所有项目通用的基础服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotBlogServices(
        this IServiceCollection services, string DbConnectionString, Assembly[]? assemblies = null)
    {
        // 未显式传入程序集时自动发现，保证仓储与 DbContext 注册在运行时和
        // dotnet-ef 设计时（入口程序集为工具自身）都能生效
        var targetAssemblies = assemblies ?? [.. ReflectionHelper.GetAllReferencedAssemblies()];

        // 1. 模块自动初始化（扫描并执行所有 IModuleInitializer）
        services.AddAutoAddInstance(targetAssemblies);

        // 2. 自动注册所有 DbContext
        services.AddAllDbContexts(ctx => ctx.UseNpgsql(DbConnectionString), targetAssemblies);

        return services;
    }
}
