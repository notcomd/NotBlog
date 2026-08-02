using Microsoft.EntityFrameworkCore;

namespace Markdown.Web.API.Extensions;

/// <summary>
///     数据库迁移执行扩展：应用启动时自动对指定 DbContext 执行 Migrate()
/// </summary>
internal static class MigrateDbContextExtensions
{
    public static IServiceCollection AddMigration<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddHostedService(sp => new MigrationHostedService<TContext>(sp));
    }

    private static async Task MigrateDbContextAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await context.Database.MigrateAsync();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}",
                typeof(TContext).Name);
            throw;
        }
    }

    private class MigrationHostedService<TContext>(IServiceProvider serviceProvider)
        : BackgroundService
        where TContext : DbContext
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return serviceProvider.MigrateDbContextAsync<TContext>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}