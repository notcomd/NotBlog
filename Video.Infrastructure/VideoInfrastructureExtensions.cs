using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Video.Domain.Cache;
using Video.Domain.IRepository;
using Video.Domain.Server;
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

        // Domain Services (with optional cache integration)
        services.AddScoped<VideoService>(sp =>
        {
            var repo = sp.GetRequiredService<IVideoRepository>();
            var cache = sp.GetService<IVideoCacheService>();
            var logger = sp.GetRequiredService<ILogger<IVideoRepository>>();
            return cache is not null
                ? new VideoService(repo, cache, logger)
                : new VideoService(repo, logger);
        });

        services.AddScoped<VideoCollectionService>();

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
}
