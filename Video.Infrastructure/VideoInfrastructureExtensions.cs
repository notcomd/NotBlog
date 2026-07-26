using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Video.Domain.Cache;
using Video.Domain.IRepository;
using Video.Domain.Server;
using Video.Domain.ValueObjects;
using Video.Infrastructure.Cache;
using Video.Infrastructure.Repository;
using Video.Infrastructure.Service;

namespace Video.Infrastructure;

public static class VideoInfrastructureExtensions
{
    public static IServiceCollection AddVideoInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IVideoCollectionRepository, VideoCollectionRepository>();

        // Domain Services — I-prefix interfaces with Infrastructure implementations
        services.AddScoped<IVideoCollectionService, VideoCollectionService>();

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

        // Infrastructure Services
        services.AddHttpClient<FileDevClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000");
            client.Timeout = TimeSpan.FromMinutes(30);
        });

        return services;
    }

    public static IServiceCollection AddVideoInfrastructure(
        this IServiceCollection services, string fileDevBaseUrl)
    {
        services.AddVideoInfrastructure();

        services.AddHttpClient<FileDevClient>(client =>
        {
            client.BaseAddress = new Uri(fileDevBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(30);
        });

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
