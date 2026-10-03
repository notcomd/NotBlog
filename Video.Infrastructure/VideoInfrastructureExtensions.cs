
namespace Video.Infrastructure;

/// <summary>
/// Video 基础设施装配扩展：注册仓储、查询服务（VideoService）、缓存服务与配置绑定。
/// </summary>
public static class VideoInfrastructureExtensions
{
    /// <summary>
    /// 注册 Video 仓储与缓存/查询服务（缓存服务存在时使用带缓存的 VideoService 构造）。
    /// </summary>
    public static IServiceCollection AddVideoInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IVideoCollectionRepository, VideoCollectionRepository>();
        services.AddScoped<IVideoHistoryRepository, VideoHistoryRepository>();

        services.AddScoped<IVideoService>(sp =>
        {
            var repo = sp.GetRequiredService<IVideoRepository>();
            var cache = sp.GetService<IVideoCacheService>();
            var logger = sp.GetRequiredService<ILogger<IVideoRepository>>();
            return cache is not null
                ? new VideoService(repo, cache, logger)
                : new VideoService(repo, logger);
        });

        // Cache Service
        services.AddScoped<IVideoCacheService, VideoCacheService>();

        return services;
    }

    /// <summary>
    /// 带 FileDev 基地址的重载（当前仅委托到无参重载，基地址保留供后续扩展）。
    /// </summary>
    public static IServiceCollection AddVideoInfrastructure(
        this IServiceCollection services, string fileDevBaseUrl)
    {
        services.AddVideoInfrastructure();

        return services;
    }

    /// <summary>
    /// 从 IConfiguration 绑定并应用 ReviewContentOptions。
    /// 应在 builder.Build() 之后、app.Run() 之前调用。
    /// </summary>
    public static void ConfigureReviewContentOptions(this IConfiguration configuration)
    {
        var options = configuration.GetSection("ReviewContent").Get<ReviewContentOptions>()
                      ?? ReviewContentOptions.Default;
        ReviewContent.Options = options;
    }
}
