
namespace Video.Infrastructure;

public static class VideoInfrastructureExtensions
{
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
