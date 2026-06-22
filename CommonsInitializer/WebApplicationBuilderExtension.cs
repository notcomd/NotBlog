using DomainInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommonsInitializer;

/// <summary>
/// 服务注册扩展
/// 统一配置所有项目通用的基础服务
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 配置 NotBlog 项目通用服务
    /// 包括：模块初始化、DbContext 注册、UnitOfWork 过滤器
    /// </summary>
    public static IServiceCollection AddNotBlogServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assemblies = ReflectionHelper.GetAllReferencedAssemblies().ToArray();

        // 1. 模块自动初始化（扫描并执行所有 IModuleInitializer）
        services.AddAutoAddInstance(assemblies);

        // 2. 自动注册所有 DbContext
        var connStr = configuration.GetValue<string>("DbContextConnect")
                      ?? configuration.GetValue<string>("DefaultDB:ConnStr")
                      ?? throw new InvalidOperationException(
                          "未找到数据库连接字符串。请在 appsettings.json 中配置 DbaseConnection 或 DefaultDB:ConnStr。");

        services.AddAllDbContexts(ctx => ctx.UseNpgsql(connStr), assemblies);

        return services;
    }

    /// <summary>
    /// 配置 NotBlog 项目通用服务
    /// 包括：模块初始化、DbContext 注册、UnitOfWork 过滤器
    /// </summary>
    public static IServiceCollection AddNotBlogServices(
        this IServiceCollection services, string DbConnectionString)
    {
        var assemblies = ReflectionHelper.GetAllReferencedAssemblies().ToArray();

        // 1. 模块自动初始化（扫描并执行所有 IModuleInitializer）
        services.AddAutoAddInstance(assemblies);

        // 2. 自动注册所有 DbContext
        services.AddAllDbContexts(ctx => ctx.UseNpgsql(DbConnectionString), assemblies);

        return services;
    }
}