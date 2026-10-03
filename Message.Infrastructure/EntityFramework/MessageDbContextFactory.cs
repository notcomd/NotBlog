using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Message.Infrastructure.EntityFramework;

/// <summary>
/// MessageDbContext 设计时工厂：供 `dotnet ef migrations` 在无宿主（未启动 Web 应用、缺 RabbitMQ 连接串）时创建 DbContext。
/// <para>仅在设计时生效，不影响运行时依赖注入；连接串为占位值（迁移文件生成不连接数据库）。</para>
/// </summary>
public class MessageDbContextFactory : IDesignTimeDbContextFactory<MessageDbContext>
{
    /// <summary>
    /// 使用占位连接串创建用于设计时的 <see cref="MessageDbContext"/> 实例。
    /// </summary>
    /// <param name="args">命令行参数（未使用）。</param>
    /// <returns>配置好的数据库上下文实例。</returns>
    public MessageDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MessageDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=messagepostgres;Port=5432;Username=postgres;Password=posgres");

        var services = new ServiceCollection();
        services.AddNotMediator(typeof(MessageDbContextFactory).Assembly);
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<INotMediator>();

        return new MessageDbContext(optionsBuilder.Options, mediator);
    }
}
