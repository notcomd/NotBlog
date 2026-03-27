using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonsInitializer;

/// <summary>
/// DbContext OptionsBuilder 工厂
/// 提供统一的数据库连接字符串配置方式
/// </summary>
public static class DbContextOptionBuilderFactory
{
    /// <summary>
    /// 从环境变量创建 OptionsBuilder（优先使用环境变量，避免配置文件泄露）
    /// </summary>
    public static DbContextOptionsBuilder<TDbContext> Create<TDbContext>()
        where TDbContext : DbContext
    {
        var connectionString = Environment.GetEnvironmentVariable("DbaseConnection")
                               ?? throw new InvalidOperationException(
                                   "未找到环境变量 DbaseConnection。请设置数据库连接字符串环境变量。");

        return new DbContextOptionsBuilder<TDbContext>().UseNpgsql(connectionString);
    }

    /// <summary>
    /// 从 IConfiguration 创建 OptionsBuilder
    /// </summary>
    public static DbContextOptionsBuilder<TDbContext> Create<TDbContext>(IConfiguration configuration)
        where TDbContext : DbContext
    {
        var connectionString = configuration.GetConnectionString("DbaseConnection")
                               ?? throw new InvalidOperationException(
                                   "未找到连接字符串 DbaseConnection。请在 appsettings.json 中配置。");

        return new DbContextOptionsBuilder<TDbContext>().UseNpgsql(connectionString);
    }
}