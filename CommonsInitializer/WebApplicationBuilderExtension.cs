using System.Reflection;
using DomainInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommonsInitializer;

/// <summary>
/// 数据库连接字符串解析器（凭据外置 S-01）：
/// 若连接串已含 Password 则原样返回；否则从环境变量 <paramref name="envPasswordName"/> 读取口令并追加；
/// 连接串缺失或口令缺失时抛出清晰错误。
/// </summary>
public static class DbConnectionStringResolver
{
    public static string Resolve(string? connectionString, string envPasswordName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "未配置数据库连接字符串（配置节 DbContextConnect / DbContextOption:DbContextConnect）。");

        if (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var password = Environment.GetEnvironmentVariable(envPasswordName);
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                $"数据库连接字符串未包含 Password，且环境变量 {envPasswordName} 未设置，无法启动。");

        return connectionString.TrimEnd(';') + $";Password={password};";
    }
}

/// <summary>
/// 服务注册扩展
/// 统一配置所有项目通用的基础服务
/// 
/// 数据库连接字符串键名规范（Q-04）：
/// 1. 优先按 DbContext 类型名独立配置：ConnectionStrings:DbContext:{ContextName}
///    （例如 ConnectionStrings:DbContext:IdentityDbContext 对应 IdentityDbContext 类型）；
/// 2. 未配置时回退到历史键名 DbContextConnect（兼容 DbContextOption:DbContextConnect）
///    与 DefaultDB:ConnStr；
/// 3. 连接串统一经 <see cref="DbConnectionStringResolver.Resolve"/> 处理（S-01 凭据外置）：
///    连接串已含 Password 则原样使用，否则从环境变量追加口令
///    （环境变量名默认 DB_PASSWORD，可用配置键 ConnectionStrings:DbContextPasswordEnv 覆盖）。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 配置 NotBlog 项目通用服务（基于 IConfiguration）
    /// 包括：模块初始化、DbContext 注册（支持按 DbContext 类型名指定独立连接串）、UnitOfWork 过滤器
    /// </summary>
    public static IServiceCollection AddNotBlogServices(
        this IServiceCollection services,
        IConfiguration configuration, Assembly[]? assemblies = null)
    {
        // 1. 模块自动初始化（扫描并执行所有 IModuleInitializer）
        services.AddAutoAddInstance(assemblies ?? Array.Empty<Assembly>());

        // 2. 自动注册所有 DbContext（连接串键名规范见类注释）
        var fallbackConnStr = configuration.GetValue<string>("DbContextConnect")
                              ?? configuration.GetValue<string>("DefaultDB:ConnStr")
                              ?? throw new InvalidOperationException(
                                  "未找到数据库连接字符串。请在 appsettings.json 中配置 DbContextConnect 或 DefaultDB:ConnStr。");

        // 口令外置（S-01）：环境变量名默认 DB_PASSWORD，可用配置键 ConnectionStrings:DbContextPasswordEnv 覆盖
        var envPasswordName = configuration.GetValue<string>("ConnectionStrings:DbContextPasswordEnv")
                              ?? "DB_PASSWORD";

        services.AddAllDbContexts(
            contextType => DbConnectionStringResolver.Resolve(
                configuration.GetValue<string>($"ConnectionStrings:DbContext:{contextType.Name}")
                ?? fallbackConnStr,
                envPasswordName),
            assemblies ?? Array.Empty<Assembly>());

        return services;
    }

    /// <summary>
    /// 配置 NotBlog 项目通用服务（直接指定连接字符串）
    /// 包括：模块初始化、DbContext 注册、UnitOfWork 过滤器
    /// </summary>
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
