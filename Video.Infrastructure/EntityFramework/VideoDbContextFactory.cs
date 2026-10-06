using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using NotMediator.Abstractions;
using NotMediator.Mediator;

namespace Video.Infrastructure.EntityFramework;

/// <summary>
/// VideoDbContext 设计时工厂：供 <c>dotnet ef migrations</c> 在无需启动 Web 宿主（不依赖 Redis/RabbitMQ 连接串）
/// 的情况下创建 DbContext。与 Message 服务的 <c>MessageDbContextFactory</c> 保持同一约定。
/// <para>仅在设计时生效，不影响运行时依赖注入；连接串为占位值（生成迁移文件不会连接数据库）。</para>
/// </summary>
public class VideoDbContextFactory : IDesignTimeDbContextFactory<VideoDbContext>
{
    /// <summary>
    /// 使用占位连接串创建用于设计时的 <see cref="VideoDbContext"/> 实例。
    /// </summary>
    /// <param name="args">命令行参数（未使用）。</param>
    /// <returns>配置好的数据库上下文实例。</returns>
    public VideoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VideoDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=videopostgres;Port=5432;Username=postgres;Password=Postgres");

        var services = new ServiceCollection();
        services.AddNotMediator(typeof(VideoDbContextFactory).Assembly);
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<INotMediator>();

        return new VideoDbContext(optionsBuilder.Options, mediator);
    }
}
