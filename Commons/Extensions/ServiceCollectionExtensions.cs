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
        // 1. 模块自动初始化（扫描并执行所有 IModuleInitializer）
        services.AddAutoAddInstance(assemblies ?? Array.Empty<Assembly>());

        // 2. 自动注册所有 DbContext
        services.AddAllDbContexts(ctx => ctx.UseNpgsql(DbConnectionString), assemblies ?? Array.Empty<Assembly>());

        return services;
    }
}
